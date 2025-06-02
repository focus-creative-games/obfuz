# Obfuz

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity Version](https://img.shields.io/badge/Unity-2019%2B-blue)](https://unity.com/)

**Obfuz** is an open-source, powerful, easy-to-use, and highly reliable Unity code obfuscation and protection solution that fully meets the demands of commercial game projects.

[English](./README-EN.md) | [中文](./README.md)

[Github](https://github.com/focus-creative-games/obfuz) | [Gitee](https://gitee.com/focus-creative-games/obfuz)

---

## Why Choose Obfuz?

- **Open Source & Free**: Licensed under MIT, free to use and modify.  
- **Powerful Features**: Delivers obfuscation and code protection comparable to commercial tools.  
- **Unity-First Design**: Deeply optimized for Unity workflows. Automatically handles all edge cases (e.g., preserving `MonoBehaviour` names) except reflection (due to technical limitations). Near-zero configuration required.  
- **Battle-Tested**: Verified by 3,000+ automated test cases covering virtually all common code patterns.  
- **Hot Reload Ready**: Fully compatible with leading hot-reload solutions like HybridCLR and xLua.  
- **Agile Development**: Rapid bug fixes, prompt feature updates, and immediate support for the latest Unity/Unity Engine changes.  

## Features

- **Symbol Obfuscation**: Supports comprehensive configuration rules and incremental obfuscation for flexible and efficient code protection.
- **Constant Obfuscation**: Obfuscates constants such as `int`, `long`, `float`, `double`, `string` and `array` to prevent reverse engineering.
- **Variable Memory Encryption**: Encrypts variables in memory to enhance runtime security.
- **Function Call Obfuscation**: Scrambles function call structures to increase cracking difficulty.
- **Randomized Encryption VM**: Generates randomized virtual machines to thwart decompilation and cracking tools.
- **Static and Dynamic Decryption**: Combines static and dynamic decryption to resist offline static analysis.
- **Seamless Unity Integration**: Deeply integrated with Unity workflows, requiring minimal configuration to get started.
- **Hot Update Compatibility**: Fully supports hot update frameworks like HybridCLR, xLua, and Puerts, ensuring compatibility with dynamic code updates.
- **DOTS Compatibility**: Works seamlessly across all DOTS versions with zero configuration required.

## Supported Unity Versions & Platforms

- Unity 2019 and later versions
- Tuanjie 1.0.0 and later versions
- All platforms supported by Unity and Tuanjie
- il2cpp and mono backend

## Planned Features

Obfuz is actively evolving. Upcoming features include:

- **Expression Obfuscation**: Obfuscate complex expressions for enhanced protection.
- **Control Flow Obfuscation**: Disrupt code flow to deter reverse engineering.
- **Code Watermarking**: Embed traceable watermarks in your code.
- **Anti-Memory Dumping and Anti-Debugging**: Prevent memory dumps and debugging attempts.
- **DLL Structure Encryption**: Secure DLL file structures against tampering.
- **Code Virtualization**: Transform code into virtualized instructions for maximum security.

## Documentation

- [Document](https://www.obfuz.com/)
- [Quick Start](https://www.obfuz.com/docs/beginner/quick-start)
- [Samples](https://github.com/focus-creative-games/obfuz-samples)

## License

Obfuz is released under the MIT License. Feel free to use, modify, and distribute it as needed.

## Contact

For questions, suggestions, or bug reports, please reach us through:

- Submit an Issue on GitHub
- Email the maintainer: [obfuz@code-philosophy.com]
- Join the ​​Luban & Obfuz Discussion Group​​ on QQ: 692890842
