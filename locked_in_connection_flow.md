
Connect  
	Oracle sends -> WebSocket Connect command(s) (async)  
	Processor sends -> confirmation “Welcome to the RTI Diagnostics Websocket server!”

Startup Chatter (async/interleaved, optional)  
	Processor sends -> Setting LogLevel on DRIVER (4) to 3  
	Processor sends -> Setting LogLevel on DRIVER (6) to 3

Subscribe to Messages  
	Oracle sends -> Subscribe to MessageLog command(s) (sent immediately after connect)  
	Processor sends -> confirmation “Echo Subscribe/MessageLog”  
	Oracle sends -> Subscribe to Sysvar command(s) (sent back-to-back after MessageLog subscribe)  
	Processor sends -> confirmation “Echo Subscribe/Sysvar”

Receive Saved Driver Log Levels  
	Processor sends -> saved log levels `{"messageType":"LogLevels","levels":[...]}`  
	Timing -> Oracle waits up to 3000 ms for this

Get Processor Time  
	Oracle sends -> “GET http://<ip>:5000/diagnostics/data/system_status” to get processor time (async HTTP)  
	Processor sends -> onDataComm()  
	Processor sends -> System Status Accessed  
	Processor sends -> memory_history with timestamp

Get Driver Names  
	Oracle sends -> “GET http://<ip>:5000/diagnostics/data/drivers” to get driver names (async HTTP)  
	Processor sends -> onDataComm()  
	Processor sends -> Drivers Status Accessed  
	Processor sends -> JSON driver list

Set Up Driver Log Level UI Status Confirmations  
	Timing gate -> Oracle waits for startup ACK chatter to settle (2000 ms quiet window, 8000 ms max)  
	Oracle sends -> `{"type":"Subscribe","resource":"LogLevel","value":{"type":"DRIVER","driverId":"<id>","level":"1"}}`  
	Oracle sends -> `{"type":"Subscribe","resource":"LogLevel","value":{"type":"DRIVER","driverId":"<id>","level":"1"}}`  
	Processor sends -> Setting LogLevel on DRIVER (<id>) to 1  
	Oracle sends -> `{"type":"Subscribe","resource":"LogLevel","value":{"type":"Diagnostics: Primary Processor","level":"0"}}`  
	Processor sends -> Setting LogLevel on Diagnostics: Primary Processor to 0  
	Timing -> protected-step ACK timeout is 7000 ms each, max 1 retry policy overall

Async behavior note  
	Processor lines can interleave across sections; Oracle outbound send order remains fixed.
