# Socnet.se: Test scripts
## Introduction to testing Socnet.se

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


## Test 1: Structural equivalence blockmodeling on binary network

This test will do structural equivalence blockmodeling on the Little League (TI) network,
which is a small binary directed network.

To run the first test, type in the following in the Socnet prompt (and press Enter):
```bash
loadscript(file = "tests/test_structural_equivalence.txt")
```
When finished, Socnet displays the optimal blockmodel, the corresponding optimal blockimage, and its
goodness-of-fit.
Make sure that the partitions are:
```bash
0: {Ron, Frank, Boyd, Tim}
1: {John, Jerry, Darrin, Ben, Arnie}
2: {Tom}
3: {Jeff, Jay, Sandy}
```
Make sure that the optimal blockimage is:
```bash
    P0  P1  P2  P3
P0  com nul nul nul
P1  nul nul nul nul
P2  nul nul nul nul
P3  nul nul com com
```
Make sure that the goodness-of-fit for the found solutioni is:
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
Make sure that the goodness-of-fit for the found solutioni is:
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

Make sure that the goodness-of-fit for the found solutioni is:
```bash
0.5071 (nordlund)
```
