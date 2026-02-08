// =============================================================================
//  vsm-connection.js — VSM Connection Manager with SignalR
// =============================================================================

// Global state
window.vsmConnection = {
    isConnected: false,
    connectedPort: null,
    availablePorts: [],
    hub: null
};

// Initialize SignalR connection
async function initSignalR() {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/testhub")
        .withAutomaticReconnect()
        .build();

    // Handle connection status updates
    connection.on("ReceiveConnectionStatus", (status) => {
        window.vsmConnection.isConnected = status.isConnected;
        window.vsmConnection.connectedPort = status.connectedPort;
        window.vsmConnection.availablePorts = status.availablePorts;
        updateConnectionUI(status);
    });

    // Handle progress messages
    connection.on("ReceiveProgress", (message) => {
        console.log("[VSM]", message);
        const log = document.getElementById("progress-log");
        if (log) {
            log.innerHTML += `<div class="log-entry">${formatTime()} ${message}</div>`;
            log.scrollTop = log.scrollHeight;
        }
    });

    // Handle test results
    connection.on("ReceiveStepResult", (result) => {
        console.log("[VSM] Step result:", result);
        if (window.onStepResult) window.onStepResult(result);
    });

    connection.on("ReceiveTestComplete", (result) => {
        console.log("[VSM] Test complete:", result);
        if (window.onTestComplete) window.onTestComplete(result);
    });

    connection.on("ReceiveSingleTestComplete", (result) => {
        console.log("[VSM] Single test complete:", result);
        if (window.onSingleTestComplete) window.onSingleTestComplete(result);
    });

    connection.on("ReceiveToken", (result) => {
        console.log("[VSM] Token:", result);
        if (window.onTokenReceived) window.onTokenReceived(result);
    });

    try {
        await connection.start();
        console.log("[VSM] SignalR connected");
        window.vsmConnection.hub = connection;

        // Get initial status
        await connection.invoke("GetConnectionStatus");
    } catch (err) {
        console.error("[VSM] SignalR connection error:", err);
    }

    return connection;
}

// Update connection UI in sidebar
function updateConnectionUI(status) {
    const dot = document.getElementById("status-dot");
    const text = document.getElementById("status-text");

    if (dot && text) {
        if (status.isConnected) {
            dot.style.background = "var(--success)";
            dot.style.animation = "pulse 2s infinite";
            text.textContent = `VSM: ${status.connectedPort}`;
            text.style.color = "var(--success)";
        } else {
            dot.style.background = "var(--error)";
            dot.style.animation = "none";
            text.textContent = "Not Connected";
            text.style.color = "var(--text-dim)";
        }
    }

    // Update port selector if present
    const portSelect = document.getElementById("port-select");
    if (portSelect && status.availablePorts) {
        const currentValue = portSelect.value;
        portSelect.innerHTML = '<option value="">Select COM Port...</option>';
        status.availablePorts.forEach(port => {
            const opt = document.createElement("option");
            opt.value = port;
            opt.textContent = port;
            if (port === status.connectedPort) opt.selected = true;
            portSelect.appendChild(opt);
        });
        if (!status.isConnected && currentValue) {
            portSelect.value = currentValue;
        }
    }

    // Update connect button if present
    const connectBtn = document.getElementById("connect-btn");
    if (connectBtn) {
        if (status.isConnected) {
            connectBtn.textContent = "Disconnect";
            connectBtn.className = "btn btn-secondary";
        } else {
            connectBtn.textContent = "Connect";
            connectBtn.className = "btn btn-primary";
        }
    }

    // Trigger custom event
    window.dispatchEvent(new CustomEvent("vsmStatusChanged", { detail: status }));
}

// Connect to COM port
async function connectVSM(portName) {
    if (!window.vsmConnection.hub) {
        console.error("[VSM] SignalR not initialized");
        return false;
    }

    try {
        await window.vsmConnection.hub.invoke("Connect", portName);
        return true;
    } catch (err) {
        console.error("[VSM] Connect error:", err);
        return false;
    }
}

// Disconnect
async function disconnectVSM() {
    if (!window.vsmConnection.hub) return;

    try {
        await window.vsmConnection.hub.invoke("Disconnect");
    } catch (err) {
        console.error("[VSM] Disconnect error:", err);
    }
}

// Run full test suite via SignalR
async function runFullTestSuite(utilityType, includeCurrency, includeKeychange, includeExtended, ea) {
    if (!window.vsmConnection.hub || !window.vsmConnection.isConnected) {
        alert("VSM not connected");
        return;
    }

    try {
        await window.vsmConnection.hub.invoke("RunFullSuite",
            utilityType, includeCurrency, includeKeychange, includeExtended, ea || 7);
    } catch (err) {
        console.error("[VSM] RunFullSuite error:", err);
    }
}

// Run single test via SignalR
async function runSingleTest(testId, utilityType, ea) {
    if (!window.vsmConnection.hub || !window.vsmConnection.isConnected) {
        alert("VSM not connected");
        return;
    }

    try {
        await window.vsmConnection.hub.invoke("RunTest", testId, utilityType, ea || 7);
    } catch (err) {
        console.error("[VSM] RunTest error:", err);
    }
}

// Generate single token via SignalR
async function generateToken(pan, reg, ti, creditType, amount, issueDate, baseDate, ea) {
    if (!window.vsmConnection.hub || !window.vsmConnection.isConnected) {
        alert("VSM not connected");
        return;
    }

    try {
        await window.vsmConnection.hub.invoke("GenerateToken",
            pan, reg, ti, creditType, amount, issueDate, baseDate, ea || 7);
    } catch (err) {
        console.error("[VSM] GenerateToken error:", err);
    }
}

// REST API fallback functions
async function fetchConnectionStatus() {
    try {
        const response = await fetch("/api/vsm/status");
        const status = await response.json();
        updateConnectionUI(status);
        return status;
    } catch (err) {
        console.error("[VSM] Status fetch error:", err);
        return null;
    }
}

async function fetchAvailablePorts() {
    try {
        const response = await fetch("/api/vsm/ports");
        return await response.json();
    } catch (err) {
        console.error("[VSM] Ports fetch error:", err);
        return [];
    }
}

// Utility
function formatTime() {
    const now = new Date();
    return `[${now.toLocaleTimeString()}]`;
}

// Initialize on page load
document.addEventListener("DOMContentLoaded", async () => {
    await initSignalR();

    // Fallback: fetch initial status via REST if SignalR fails
    setTimeout(async () => {
        if (!window.vsmConnection.isConnected && !window.vsmConnection.connectedPort) {
            await fetchConnectionStatus();
        }
    }, 2000);
});
