# Building the Windows Package

This is how I build the lean, mean Windows binary and self-contained runtime (20MB).

I run it inside WSL2, but you don't have to.

````
   dotnet publish -r win-x64 \
                -p:PublishTrimmed=true \
                -p:PublishSingleFile=true \
                --self-contained true \
                -o /tmp/PhotoReviewer4Net
````

