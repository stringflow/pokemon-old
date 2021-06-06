# Pokemon

A work-in-progress libgambatte wrapper designed for the generation 1 and 2 games of the Pokemon series.  
Mainly used for bruteforce searching of RTA rng manipulations and theory TASing for now.


# Build Instructions (64-bit only for now)

1. `git clone https://github.com/stringflow/pokemon`
2. Download the prebuilt 64-bit binary of [SDL2](https://www.libsdl.org/download-2.0.php) from their website and copy it to the root directory of the project.
3. Create a folder called `roms` and add ROM images to it. The files must be named `poke*.gbc` (i.e. `pokered.gbc`, `pokecrystal.gbc`, etc.).
4. Open the project in vscode and run!