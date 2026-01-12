// ws-probe.js
// Full WebSocket diagnostics probe for RTI Smart Home Processor
// Includes Sysvar (variables) discovery

const WebSocket = require('ws');

const ip = "192.168.1.143";
const port = 1234;
const url = `ws://${ip}:${port}/diagnosticswss`;

console.log(`Connecting to RTI WebSocket at ${url}...\n`);

const ws = new WebSocket(url, {
    headers: {
        "Origin": `http://${ip}`
    }
});

// Confirmed WS resources
const resources = [
    { name: "MessageLog", value: "true" },
    { name: "LogLevel", value: { type: "EVENTS_INPUT", level: "3" } },
    { name: "UPnP", value: "true" },
    { name: "Flags", value: "true" }
];

// Sysvar discovery section
const sysvarStart = 1;          // First Sysvar ID to probe
const sysvarEnd   = 64;         // Probe first 64; adjust as needed
let sysvarEnabled = true;       // Turn brute-force on/off

// Unknown resource probes
const probeNames = [
    "Sysvar",
    "MessageLog",
    "UPnP",
    "LogLevel",
    "Flags"
];

// Subscribe helper
function subscribe(resource, value) {
    const payload = {
        type: "Subscribe",
        resource,
        value
    };
    ws.send(JSON.stringify(payload));
    console.log(`Subscribed: ${resource} ->`, value);
}

// Brute-force Sysvar enumeration
function probeSysvars() {
    console.log(`\n--- Probing Sysvar IDs ${sysvarStart} through ${sysvarEnd} ---\n`);
    for (let id = sysvarStart; id <= sysvarEnd; id++) {
        ws.send(JSON.stringify({
            type: "Subscribe",
            resource: "Sysvar",
            value: { id: id, status: true }
        }));
    }
}

ws.on('open', () => {
    console.log("WebSocket connection established.\n");

    // handshake test
    ws.send(JSON.stringify({ type: "echo", message: "probe-start" }));

    console.log("\nSubscribing to confirmed resources...\n");
    resources.forEach(r => subscribe(r.name, r.value));

    console.log("\nProbing known resource names...\n");
    probeNames.forEach(name => {
        subscribe(name, "true");
    });

    if (sysvarEnabled) {
        probeSysvars();
    }

    console.log("\n--- WS Probe Running ---\n");
});

// Message handler
ws.on('message', (data) => {
    let text = data.toString();
    try {
        let json = JSON.parse(text);
        console.log("\n===== WS MESSAGE =====");
        console.log(JSON.stringify(json, null, 2));
        console.log("======================\n");
    } catch {
        console.log("\n[Non-JSON Message]");
        console.log(text);
    }
});

// Close handler
ws.on('close', () => {
    console.log("WebSocket connection closed.");
});

// Error handler
ws.on('error', (err) => {
    console.error("WebSocket error:", err);
});


// // ws-probe.js
// // Full WebSocket diagnostics probe for RTI Smart Home Processor
// // Connects, enumerates known resources, subscribes, listens, logs structures

// const WebSocket = require('ws');

// const ip = "192.168.1.143";
// const port = 1234;
// const url = `ws://${ip}:${port}/diagnosticswss`;

// console.log(`Connecting to RTI WebSocket at ${url}...\n`);

// const ws = new WebSocket(url, {
//     headers: {
//         "Origin": `http://${ip}`
//     }
// });

// // Known RTI WebSocket resources (expandable)
// const resources = [
//     // known from reverse engineering
//     { name: "MessageLog", value: "true" },
//     { name: "LogLevel", value: { type: "EVENTS_INPUT", level: "3" } },
//     { name: "UPnP", value: "true" },
//     { name: "Variables", value: "true" },
//     { name: "Flags", value: "true" },
// ];

// // Hidden discovery patterns
// const probeQueries = [
//     "MessageLog",
//     "UPnP",
//     "LogLevel",
//     "Variables" ,
//     "Flags",
//     "Drivers",
//     "RTiPanel",
//     "Zigbee",
//     "Events",
//     "System",
// ];

// function subscribeTo(resource) {
//     let payload = {
//         type: "Subscribe",
//         resource: resource.name,
//         value: resource.value
//     };

//     ws.send(JSON.stringify(payload));
//     console.log(`Subscribed to ${resource.name}`);
// }

// ws.on('open', () => {
//     console.log("WebSocket connection established.\n");

//     console.log("Sending echo check...");
//     ws.send(JSON.stringify({ type: "echo", message: "probe-start" }));

//     console.log("\nSubscribing to known resources...\n");
//     resources.forEach(res => subscribeTo(res));

//     console.log("\nProbing for unknown RTI WS resources...\n");

//     probeQueries.forEach(q => {
//         ws.send(JSON.stringify({
//             type: "Subscribe",
//             resource: q,
//             value: "true"
//         }));
//     });
// });

// ws.on('message', (data) => {
//     let text = data.toString();

//     try {
//         let json = JSON.parse(text);

//         console.log("\n===== MESSAGE RECEIVED =====");
//         console.log(JSON.stringify(json, null, 2));
//         console.log("============================\n");

//     } catch (err) {
//         console.log("\n[Non-JSON Message]");
//         console.log(text);
//     }
// });

// ws.on('close', () => {
//     console.log("WebSocket connection closed.");
// });

// ws.on('error', (err) => {
//     console.error("WebSocket error:", err);
// });
