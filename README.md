# LCS_Unity
A graphical remake of Liberal Crime Squad in Unity

Setup instructions:
1) Clone the respository to some local folder
2) Import that folder as a project by selecting "add" in the Unity Hub (available from https://unity3d.com/get-unity/download)
2a) (optional) The version of Unity this was built on is 2018.3.8f1, so downloading that version is ideal. It should still be compatible with newer versions, however, as it mostly does not use Unity features
3) Open the project and load the "Game" scene

The entire UI lives in the Game scene under the Main Camera->Canvas->Inner Border element - individual screens can be edited by enabling them in the editor. All top level screens should be disabled before doing a build, leaving MapMask as the only active element under the Inner Border.
