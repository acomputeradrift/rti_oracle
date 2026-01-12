const WebSocket = require('ws');

const ws = new WebSocket('ws://192.168.1.143:1234/diagnosticswss');

ws.on('open', () => {
  console.log('Connected to the RTI Smart Home server.');
  
  ws.send(JSON.stringify({
    type: "Subscribe",
    resource: "MessageLog",
    value: "true"
  }));
});

ws.on('message', (data) => {
  try {
    const message = JSON.parse(data.toString('utf8'));
    console.log('Received:', JSON.stringify(message, null, 2));
  } catch (error) {
    console.error('Error parsing message:', error);
  }
});

ws.on('error', (error) => {
  console.error('WebSocket error:', error);
});

ws.on('close', () => {
  console.log('Disconnected from the RTI Smart Home server.');
});


