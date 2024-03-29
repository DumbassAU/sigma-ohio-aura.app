using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AOULauncher;

[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(LauncherConfig))]
internal partial class LauncherConfigContext : JsonSerializerContext;

[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(List<FileHash>))]
internal partial class FileHashListContext : JsonSerializerContext;