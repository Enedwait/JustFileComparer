### DISCLAIMER 
The app and repo are not finished yet and still in development though it is in its final stages.

# JustFileComparer
A small utility to recursively compare files from source folder with target folder.
This is the cross-platform C# utility with a GUI (Avalonia).

## Purpose
I wanted to have a simple application which is able to compare contents of one folder with another after making a backup copy because I do these kinds of copies pretty often. For this current version I decided to compare only the sizes and hashes; it seems fine enough for my goal.

## Features
0) Cross-platform [rumored] - well, I am using Windows and haven't checked other OSs yet, but I hope it works! ^_^
1) Parallel file enumeration - by default it uses all logical processors to search files inside the source folder.
2) Asynchronous and streamlined file comparison - as soon as a new source file is found it is being processed (compared with the target file)
3) MVVM

##### That's all for now
