// RTI Diagnostics Endpoint Probe
// Queries all known diagnostics endpoints and prints available information.

const http = require("http");

const IP = "192.168.1.143";     // change if needed
const PORT = 5000;              // diagnostics HTTP port

const endpoints = [
  "dashboard",
  "drivers",
  "rtipanel",
  "zigbee",
  "upnp",
  "variables",
  "flags",
  "systemlog"
];

function fetchEndpoint(ep) {
  const options = {
    hostname: IP,
    port: PORT,
    path: `/diagnostics/data/${ep}`,
    method: "GET",
    timeout: 3000
  };

  const req = http.request(options, (res) => {
    let data = "";

    res.on("data", (chunk) => data += chunk);
    res.on("end", () => {
      const preview = data.length > 250 ? data.substring(0, 250) + "..." : data;

      console.log(`\n===== ${ep.toUpperCase()} =====`);
      console.log(`Status: ${res.statusCode}`);
      console.log(`Content-Type: ${res.headers["content-type"] || "unknown"}`);
      console.log(`Length: ${data.length} bytes`);

      if (res.statusCode === 200) {
        console.log("Preview:");
        console.log(preview);
      } else {
        console.log("Endpoint not available.");
      }
    });
  });

  req.on("error", (err) => {
    console.log(`\n===== ${ep.toUpperCase()} =====`);
    console.log(`ERROR: ${err.message}`);
  });

  req.on("timeout", () => {
    req.destroy();
    console.log(`\n===== ${ep.toUpperCase()} =====`);
    console.log("ERROR: Request timed out");
  });

  req.end();
}

// Run all endpoint queries sequentially
console.log("Querying RTI diagnostics endpoints...\n");
endpoints.forEach(fetchEndpoint);
