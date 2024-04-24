# Unity CRC32 Bypass

A tool to bypass Unity's CRC32 check for BundleFiles by appending extra data to the end of the file.

一个通过在ab文件末端添加额外数据来绕过Unity的CRC32检查的工具。

## Usage

```shell
Usage: ./Unity_CRC32_Bypass <original file path> <modified file path> <output path>
```
`original file path` - The path to the original BundleFile without any modification.原始的未经修改的BundleFile的路径。

`modified file path` - The path to the modified BundleFile you want to apply to the game.经过修改的你想要应用到游戏中的BundleFile的路径。

`output path` - The path to the output file.输出的文件路径。

***Attention:*** All files in this repository are uncompressed files, and it does not support directly operating ab files compressed with lz4/lzma for now.

***注意:*** 这里出现的所有ab文件都是解压后的文件，暂时不支持直接操作lz4/lzma压缩的ab文件。

## How it works

参考看雪论坛上的[这篇文章](https://bbs.kanxue.com/thread-8699.htm)实现CRC32碰撞。

