# ObsidianWorldConverter
This Program allows you to convert the following Compression Algorithms used for compressing NBT in Region Files to each other.
Supported NBT Compressions Algorithms: None, GZip, ZLib, ZStd, Brotli.

## Notchian vs Obsidian Implementation
The Notchian server will always use ZLib but can read GZip and ZLib compressed Region Files.
See [Wiki.vg](https://wiki.vg/Map_Format#.5Bregion.5D.mca:~:text=The%20notchian%20implementation%20will%20never%20compress%20with%20gzip%20but%20can%20read%20if%20provided.) for more Info.
Obsidian will automatically detect the Compression Algorithm that is used and Supports all Algorithms that this Program supports.

## Usage
### Convert from Notchian Server to ZStd
Example: `ObsidianWorldConverter.exe --work-directory region --input-format None --output-format ZStd`
### Convert from Brotli to GZip
Example: `ObsidianWorldConverter.exe --work-directory region --input-format Brotli --output-format GZip`
