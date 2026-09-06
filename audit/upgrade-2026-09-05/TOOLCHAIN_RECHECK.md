# Toolchain check — September 5, 2026

The upgraded build retains the existing coherent Godot .NET 4.6.3 / net8.0 toolchain and .NET SDK 8.0.424. It does not claim to use the newest Godot minor version.

Godot's [current release policy](https://docs.godotengine.org/en/stable/about/release_policy.html) lists the 4.6 branch as supported; the [4.6.3 archive](https://godotengine.org/download/archive/4.6.3-stable/) provides its matching editor and export templates. Microsoft's [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy) lists .NET 8 in maintenance through November 10, 2026. A future release past that boundary needs a supported target/runtime migration and fresh executable evidence.

CI pins action revisions and SDK/Python versions. The normal job runs source contracts and actual managed integration tests. Native export/input verification is a separate manual job on a configured Windows runner with matching Godot templates. Neither job certifies physical controllers, two physical PCs or human feel.
