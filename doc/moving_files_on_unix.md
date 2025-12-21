# Moving Files on UNIX

If you want to move a bunch of files to *one* target directory, that is
easy enough.  Just be SURE that:

* ⚠  Your file names are uniquely numbered or timestamped, and don't collide.

* ⚠  You are OK with losing the subdirectory names, as shown below:

````
Original locations:

  ~/Pictures/2025/01 January/IMG_0001.jpg
  ~/Pictures/2025/02 Feburary/IMG_0002.jpg
  ~/Pictures/2025/03 March/Skiing in Colorado/IMG_0005.jpg
  ~/Pictures/2025/03 March/Skiing in Colorado/IMG_0007.jpg

New location:

  /tmp/badfiles/IMG_0001.jpg
  /tmp/badfiles/IMG_0002.jpg
  /tmp/badfiles/IMG_0005.jpg
  /tmp/badfiles/IMG_0007.jpg
````


## Linux or GNU

Note: Mac OS does not have `mv -t`, so this will not work unless you
install the GNU coreutils version.

````sh
# Get files with a "Bad" rating (null terminated)
photo_reviewer_4net --print0 /tmp/ratings.json Bad > /tmp/files.txt

# Move files to the target directory /tmp/badfiles
# CAREFUL: Any subdirectory information is lost, they all go to one directory!
mkdir /tmp/badfiles
cat /tmp/files.txt | xargs -0 mv -v -n -t /tmp/badfiles
````

Note: `-v` = verbose output, `-n` = no clobber (accidental overwrite), `-t` =
target directory.


## Mac Only

Mac OS has a special `xargs -J` option and its [man page example](https://ss64.com/mac/xargs.html)
is about moving files to a target directory.

So, you can investigate that method on your own, or... just install the GNU
coreutils.
