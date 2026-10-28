# AGENTS.md

Notes for anyone, human or AI agent, changing this code. There is no README; this file is the project's
documentation.

## What this is

SamplePDFScanner, a small C# console program on .NET 9 that pulls the text out of a PDF by asking a local
language model. It reads the PDF, encodes it as Base64, and sends it to LM Studio's OpenAI-style chat endpoint
(`http://localhost:1234/v1/chat/completions`). It then prints the model's reply and the token counts.

## Commands

```bash
dotnet build SamplePDFScanner.sln                                  # no global.json: any .NET 9 or later SDK
dotnet run --project SamplePDFScanner -- path/to/file.pdf          # needs LM Studio running on localhost:1234
```

The only argument is the path to a `.pdf` file. There are no tests.

## Layout

- `SamplePDFScanner/Program.cs`: everything.
  - `RequestPayload`, `Message`, `ContentItem` and `ImageUrl` model the chat request; `ApiResponse`, `Choice`,
    `ResponseMessage` and `Usage` model the reply. All use `System.Text.Json` with explicit `JsonPropertyName`s.
  - `Program.Main` checks the argument; `ExtractTextFromPdf` builds the request, posts it and returns the text.
- `SamplePDFScanner/SamplePDFScanner.csproj`: `net9.0`, nullable and implicit usings on, `PublishAot` and
  `InvariantGlobalization` set.
- `SamplePDFScanner/bin/` and `obj/`: build output from an early commit, still tracked in git. A build rewrites
  these files; don't commit the changes.
- `.idea/`: Rider settings.

## Rules that keep it working

- **The endpoint and model are constants.** `LM_STUDIO_URL` and the model name (`qwen3-32b`) are hard-coded in
  `Program.cs`. The model must be one loaded in LM Studio, and it must accept a PDF sent as an `image_url` data
  URI.
- **Keep the JSON names.** The `JsonPropertyName` attributes match the OpenAI chat format (`max_tokens`,
  `image_url`, `finish_reason`); renaming the C# properties is safe, changing the attributes is not.
- **AOT.** `PublishAot` is on, but serialization uses the reflection-based `DefaultJsonTypeInfoResolver`. Check a
  native-AOT publish still works after changing the JSON classes.
- **Encoding.** `Program.cs` and the `.csproj` are UTF-8 with a byte-order mark; keep them that way.

## Making a change

- Build, then run it against a real PDF with LM Studio running, and read the output.
- There is no CI workflow.
