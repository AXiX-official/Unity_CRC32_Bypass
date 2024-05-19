# Unity CRC32 Bypass

A tool to bypass Unity's CRC32 check for BundleFiles by appending extra data to the end of the file.

一个通过在ab文件末端添加额外数据来绕过Unity的CRC32检查的工具。

## Usage

```shell
./Unity_CRC32_Bypass <original file path> <modified file path> <output path>  <compress type[default: none]>
```
- `original file path` - The path to the original BundleFile without any modification.原始的未经修改的BundleFile的路径。

- `modified file path` - The path to the modified BundleFile you want to apply to the game.经过修改的你想要应用到游戏中的BundleFile的路径。

- `output path` - The path to the output file.输出的文件路径。

- `compress type` - The compress type of the output file.输出文件的压缩类型。

    - `none` - No compress.不压缩。

    - `lz4` - Use lz4 compress.使用lz4压缩。

    - `lz4hc` - Use lz4hc compress.使用lz4hc压缩。

    - `lzma` - Use lzma compress.使用lzma压缩。

For UnityCN encrypted files consider exporting non-encrypted files using [UnityCN-Helper](https://github.com/AXiX-official/UnityCN-Helper) before processing them with this tool.
UABEA can't handle encrypted files, and UnityPy will only export decrypted files, so this tool won't consider processing UnityCN encrypted files directly (although it can fully support it).

对于UnityCN加密的文件考虑先使用[UnityCN-Helper](https://github.com/AXiX-official/UnityCN-Helper)导出非加密的文件再使用本工具处理。
UABEA无法处理加密的文件，而UnityPy只会导出解密后的文件，所以本工具不会考虑直接对UnityCN加密的文件进行处理（虽然完全可以支持）。

## How it works

参考看雪论坛上的[这篇文章](https://bbs.kanxue.com/thread-8699.htm)实现CRC32碰撞。

## Libraries

- [UnityAsset.NET](https://github.com/AXiX-official/UnityAsset.NET)

