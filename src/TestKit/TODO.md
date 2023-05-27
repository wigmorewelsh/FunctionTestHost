TODO
----
3. Enable nuget package checking - from 1.0
4. Really basic support for calling service bus in bindings
5. Handle error from function invoke
6. Concurrent tests to check consistancy
7. Sort out release notes

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
6. Setup versioning
1. Populate parameter bindings from metadata
4. Support different argument names
6. Allow tests to override parts of the function app config
3. Send http message body to function
