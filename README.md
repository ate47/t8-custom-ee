# t8-custom-ee
A tool to allow EEs in local or custom mutation in Black Ops 4 ([Demo video](https://www.youtube.com/watch?v=zGUf-qEcHMU))

Load the zombies mode and press "Inject mod", you can remove the mod using "Reverse mod". (only in the menu otherwise you might crash)

This mod is using the knowledge of the [t8-compiler by Serious](https://github.com/shiversoftdev/t7-compiler), it replaces the `function_e51dc2d8`'s bytecode to `return true` to enable step EEs even if you are in casual, local or custom mutation.

This mod is a temp modification knowing the detour API isn't available for Black ops 4 in the t8-compiler.

## Known issues

- leaving the zombies mode in the menu erase the mod, you need to reinject
- the "wheel" of the main EE isn´t visible, for the same reason some UI elements might not be available
