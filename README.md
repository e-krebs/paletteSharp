# paletteSharp
> paletteSharp is a simple port of Android's Palette library (and its dependencies)

## Demo
The solution contains a command-line demo project which you can run after compiling :
```shell
demo path_to_a_file_to_analyse.png
```
This demo project support any image type that the Image.FromFile supports, which means : bmp, gif, jpeg, png and tiff

The output should look like this :
```shell
swatches:
Dominant: #C8A068 - population: 1584
        text colors - title: #000000 - body: #000000

Vibrant: #C06838 - population: 813
        text colors - title: #000000 - body: #000000
DarkVibrant has no swatch
Muted: #807868 - population: 1571
        text colors - title: #000000 - body: #000000
DarkMuted: #504040 - population: 785
        text colors - title: #FFFFFF - body: #FFFFFF
LightVibrant: #C8A068 - population: 1584
        text colors - title: #000000 - body: #000000
LightMuted: #C8B088 - population: 788
        text colors - title: #000000 - body: #000000
```

## Install
paletteSharp is available as a nuget package on nuGet.org at https://www.nuget.org/packages/PaletteSharp/
