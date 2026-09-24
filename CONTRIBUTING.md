# Contributing
We welcome contributions from the community. Here are some ways you can contribute to the project.

## Reporting Issues
If you find a bug or have a problem with the software, please open an issue on our GitHub repository.

When reporting an issue, please include the following information:

- A clear and descriptive title.
- A detailed description of the issue, including steps to reproduce it.
- The version of the software you are using.
- Any relevant error messages or logs.

## Contributing Code
If you would like to contribute code to the project, please follow these steps:

- Contact me! <carl.nordlund@liu.se>
- Fork the repository.
- Create a new branch for your feature or bug fix.
- Make your changes and commit them with a clear and descriptive commit message.
- Push your changes to your fork.
- Open a pull request to the main repository.
- Please ensure that your code adheres to the existing coding style and that all tests pass.
## Code structure
- `Socnet.Core/Model`: the data structures (Actorset, Matrix, Partition, BlockImage, BlockModel, ...) and the Dataset holding them.
- `Socnet.Core/Blocks`: the ideal blocks.
- `Socnet.Core/Blockmodeling`: the search state, the search algorithms, blockimage varieties and the evaluation of blockmodels.
- `Socnet.Core/IO` and `Socnet.Core/Processing`: loading/saving files, and matrix transformations.
- `Socnet.CLIconsole`: the console application. Each command is registered in `Commands/CommandRegistry.cs`.
- `Socnet.Tests`: automated tests (see [TESTING.md](TESTING.md)).

## Adding a new ideal block
1. Create a subclass of `IdealBlock` in `Socnet.Core/Blocks` (see the existing blocks and the documentation of
   `IdealBlock`), with a unique name and a unique `IsoIndex`.
2. Implement `Hamming`/`WriteIdealHamming` and/or `Nordlund`/`WriteIdealNordlund`, and set `SupportsHamming` and
   `SupportsNordlund` accordingly.
3. Optionally, implement the fast paths `TryHamming`/`TryNordlund`, which compute the same values from aggregated
   block statistics in constant time. Searches use these when available.
4. Register the block in `BlockFactory` (in `IdealBlock.cs`).
5. Add tests, for instance a variant of `IncrementalState_MatchesFullEvaluation` (in `Socnet.Tests/BlockEquivalenceTests.cs`)
   that uses the new block, checking that the fast incremental evaluation agrees with the full evaluation.
   (The block lists in `TestData` are also used to compare with version 1.4, so they should only contain blocks that exist in 1.4.)
