# Sapwood.IO.FileFormats

A decoder library for JPEG (standard and progressive), MP3 (CBR/VBR/ABR), PNG, GIF, and more.

# Why?

- You may want headless conversion of file formats without platform-dependent code. By headless I mean, not tied to a graphics context specific to any operating system, such as gdi32.dll for Windows for example. 
- This is good for both console apps and web servers.
- It's 100% managed .NET code with no unsafe memory calls.
- This is also good for reading in the tags associated with the above mentioned file formats.

There's lots more to do including TTF, SVG, and PDF support, encoders for formats, etc.
