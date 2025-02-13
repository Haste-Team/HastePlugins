Haste example plugins
===

Start by looking at the HelloWorld project. HelloWorld.csproj is a nice template for you to use to get started developing.

## Basic information (how to create and upload C# mods)

- Press F1 to open the debug menu. The tab "Steam Workshop" lists workshop items you're subscribed to.
- There's a setting in there per-mod called `Local override dir` that you can set to redirect the mod from the Steam directory to any directory on your computer. I typically have it set to the `C:\...\MyMod\bin\Debug\netstandard2.1` folder.
- There is no concept of a "local `Plugins` directory" mods anymore (as in Content Warning). Development is expected to be done through `Local override dir` before uploading. (This is so that "local" mods respect the steam item load order)
- The tab  "Steam Workshop Uploader" is hopefully self-explanatory. Check or uncheck the boxes to modify or not modify that field. For routine mod updates, I imagine only the `Set content?` tickbox will be checked.
- To start developing your own mod, use the Uploader to create an empty mod (only setting title/description, for example), subscribe to it, then set the `Local override dir` to your mod's build directory.
- Any type marked with `[LandfallPlugin]` will have its static constructor ran on mod load, after the game has booted. All mod assemblies are loaded, and THEN all static constructors are executed, hopefully resolving a lot of load order issues.
- [Module initializers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/module-initializers) are executed at startup by the preloader before Assembly-CSharp/etc. is loaded. `CecilPatcher.EditAssembly("Assembly-CSharp")` is a nice/easy way to edit assemblies during the preload phase in your module initializer. `CecilPatcher.Myself()` gives you a reference to yourself as a Cecil AssemblyDefinition, useful as directly referencing your patches via reflection may inadvertently load Assembly-CSharp.
- Harmony/etc. is not shipped by the base game. If you want to use it (or any other dependency), upload Harmony as a workshop item you depend on (the community has already done this), or bundle it with your mod. Cecil *is* shipped by the base game, to support `CecilPatcher.EditAssembly`.

## Creating custom items (no coding required)

1) Open up the F1 Items menu
2) Modify an item
3) Press "export to \[blah\].hasteitem.json" button
4) Rename the file to YourItemName.hasteitem.json (replacing YourItemName with... your item name)
5) (optional): Edit the .json file directly (it's relatively obvious, and basically mirrors the ingame thing directly)
6) (optional): Use the "import from \*.hasteitem.json" button, and once imported, the "Give item" button to test ("Give item" button is equivalent to the console command `Items.AddItem itemname`)
7) Include the YourItemName.hasteitem.json file in your Workshop item (no code whatsoever needs to be in the workshop item, just the \*.hasteitem.json is a valid "mod" on its own)
8) The modloader adds any \*.hasteitem.json files found in subscribed workshop items to the item database, making them available ingame.

### Creating 3d models for your custom items (a default model is used if you do not provide one):

1) Create a mesh
2) Import it into a Unity project
3) Ensure the mesh object is named identically to your custom item (not the gameobject, the actual Mesh object)
4) Also create a mesh named `YourItemName.001`, to be used by the outline shader.
5) Create an [Asset Bundle](https://docs.unity3d.com/Manual/AssetBundlesIntro.html) containing the mesh
6) Give the asset bundle the file extension `.hasteitem.assetbundle` (the specific filename does not matter)
7) The modloader loads any \*.hasteitem.assetbundle files found in subscribed workshop items. All Mesh objects found within whose names match an item will be used for that item's mesh.
8) (Advanced): All `ItemInstance` monobehaviour components found within the asset bundle are also added to the item database, as an advanced alternative to using a \*.hasteitem.json file.

## Modifying vanilla/builtin items

Go through the workflow as above, but name the file the same as a vanilla item (e.g. do not rename the file). Any \*.hasteitem.json files whose name matches an existing item will not create a new item but instead override the properties of that existing item. Optionally, delete fields in your .json that you did not modify, to inherit the original values from the vanilla item (i.e. don't override them).

For example, this file modifies the vanilla "Golden Necklace" item to give a 100% speed boost instead of 10%:

`PermanentBoost.hasteitem.json`:

```json
{
  "description": "Gain 100% Speed Boost.",
  "stats": {
    "boost": {
      "baseValue": 1.0
    }
  }
}
```

## Localization

Sometimes, you want to modify or create a new LocalizedString. You can create a new localization table by providing a \*.localization.json file in your workshop item:

`MyLocTable.localization.json`:

```json
{
  "myKey": "hello!",
  "anotherKey": "goodbye!"
}
```

You can specify strings for specific locales instead of a single unlocalized string via:

```jsonc
{
  "myKey": {
    "default": "hi", // used if no others match
    "en-US": "hello",
    "sv": "hej" // fallbacks are iteratively searched, so a user's culture of sv-SE will also try sv
  }
}
```

`MyLocTable.localization.json` will create a localization table called `MyLocTable`. Use it in code as `new LocalizedString("MyLocTable", "myKey")`, or in a \*.hasteitem.json like:

```json
{
  "title": {
    "m_TableReference": {
      "m_TableCollectionName": "MyLocTable"
    },
    "m_TableEntryReference": {
      "m_Key": "myKey"
    }
  }
}
```
