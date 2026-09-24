# Socnet.se: Testing

## Test scripts
The repository contains 3 Socnet-scripts to test core analytical features in Socnet.se.
Note that these scripts are Socnet.se scripts, meaning that they are text files containing
a sequence of Socnet.se-specific CLI commands that can be entered into the Socnet.se CLI console.

However, it is easier to use the CLI command `loadscript(..)` to load and execute a script file.

Before running the script file corresponding to each test, do the following:

- Start the Socnet client
- Make sure that the working directory is the parent folder of where the folders `/example_data/`
and `/tests/` are located (i.e. you should be in the root folder of the project repository)

If not, set the current working directory using the following CLI command:
```bash
setwd(dir = "[path_to_root_folder]")
```
Check that you are in the correct folder by using the `dir()` command:
```bash
dir
```
The `/example_data/` and `/tests/` folders should be visible in the list that appears in the Socnet client.

- **Note**: The `/example_data/` folder also contains a more comprehensive script called `cli_script.txt`.
  This script is meant more as a tutorial to Socnet commands. There is also additional documentation and a
  quick-start guide on [https://www.socnet.se](https://www.socnet.se).

## Test 1: Structural equivalence blockmodeling on binary network

This test will do structural equivalence blockmodeling on the Little League (TI) network,
which is a small binary directed network.

To run the first test, type in the following in the Socnet prompt (and press Enter):
```bash
loadscript(file = "tests/test_structural_equivalence.txt")
```
When finished, Socnet displays the optimal blockmodel, the corresponding optimal blockimage, and its
goodness-of-fit. Note that the order of the positions (P0, P1, ...) can differ between runs and versions:
the solutions are the same up to this ordering.
Make sure that the partitions are:
```bash
0: {Jeff, Jay, Sandy}
1: {John, Jerry, Darrin, Ben, Arnie}
2: {Tom}
3: {Ron, Frank, Boyd, Tim}
```
Make sure that the optimal blockimage is:
```bash
    P0  P1  P2  P3
P0  com nul com nul
P1  nul nul nul nul
P2  nul nul nul nul
P3  nul nul nul com
```
Make sure that the goodness-of-fit for the found solution is:
```bash
20 (hamming)
```


## Test 2: Regular equivalence blockmodeling on valued network

This test will do regular equivalence blockmodeling on the Hlebec notesharing network,
which is a small valued directional network.

To run the second test, type in the following in the Socnet prompt (and press Enter):
```bash
loadscript(file = "tests/test_regular_equivalence.txt")
```
When finished, Socnet displays the optimal blockmodel, the corresponding optimal blockimage, and its
goodness-of-fit.
Make sure that the partitions are:
```bash
0: {4,8,9}
1: {2,10}
2: {1,3,5,6,7,11,12,13}
```
Make sure that the optimal blockimage is:
```bash
    P0  P1  P2
P0  reg nul nul
P1  reg reg nul
P2  reg nul nul
```
Make sure that the goodness-of-fit for the found solution is:
```bash
0.8813 (nordlund)
```


## Test 3: Identifying power-relational core-periphery structures

This test will search for core-periphery structures in the original Baker valued citation data,
with peripheral dependency and a p-core (p=0.75), using the ljubljana search algorithm

To run the third test, type in the following in the Socnet prompt (and press Enter):
```bash
loadscript(file = "tests/test_coreperiphery.txt")
```
When finished, Socnet displays the optimal blockmodel, the corresponding optimal blockimage, and its
goodness-of-fit.
Make sure that the partitions are:
```bash
Core: {CW,CYSR,SCW,SSR,SW}
Periphery: {remaining 15 journals}
```
The pre-specified blockimage is as follows:
```bash
     C          P
C    pco(0.75)  cfn
P    rfn        nul
```
Note: cfn=column-functional ideal block, rfn=row-functional ideal block

Make sure that the goodness-of-fit for the found solution is:
```bash
0.5071 (nordlund)
```

## Automated tests
The repository contains a test project, `Socnet.Tests`, with automated unit tests. It requires the .NET 10 SDK and is run from the root folder of the repository with:
```bash
dotnet test
```
All tests should pass. The automated tests cover:

- **Equivalence with Socnet.se 1.4**: the ideal block code of version 1.4 is kept unchanged in `Socnet.Tests/Legacy/` and used as a reference.
  For thousands of random networks, partitions and blockimages, the goodness-of-fit values and ideal matrices of all ideal blocks
  (for both `hamming` and `nordlund`) must be identical to those of version 1.4.
- **Incremental evaluation**: the fast incremental evaluation used during searches must always give the same goodness-of-fit as a full
  evaluation of the blockmodel.
- **Searches**:
  - the exhaustive search must find the true optimum, compared with brute force;
  - the `localopt` and `ljubljana` searches must find the known optima of the test scripts below;
  - results must be reproducible for a given random seed, regardless of the number of threads used;
  - blockimage varieties must be non-isomorphic;
  - timeouts must be reported.
- **Console compatibility**: `Socnet.Tests/Golden/deterministic_script.txt` runs all non-search commands. Its console output must be
  identical to the output of version 1.4 (`deterministic_script.v14.out`), apart from a few deliberate differences documented in
  `ConsoleCompatibilityTests.cs`: the version string, a header-line fix in table views, and the actor order in partitions modified with `set()`.
- **Commands**:
  - the command parser, including nested brackets such as `intercat = denuci(0.5)` and the error messages for invalid commands;
  - the `threads` argument;
  - the `system()` command.




