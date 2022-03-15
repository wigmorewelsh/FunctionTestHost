TODO
----
1. Populate parameter bindings from metadata
2. Clone port randomizer from orleans
3. Send http message body to function
4. Support different argument names
5. Enable nuget package checking
6. Setup versioning

Test Server
-----------

8. Load each app function in seperate AssemblyLoadContext
9. Ensure test host shuts down correctly

Done:
-----

1. Testhost to wrap, startup and call Program.cs
2. Pass back bindings to server
3. Setup stream from grain to grpc
4. Call function stream to start function from grain
5. Add function grain to registory
7. Return http resposne back to caller
