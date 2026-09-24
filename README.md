[![status](https://joss.theoj.org/papers/650cd553bf6a3f9cb5e8bc5003d1c420/status.svg)](https://joss.theoj.org/papers/650cd553bf6a3f9cb5e8bc5003d1c420)

# Socnet.se

### About
Socnet.se is an open-source, CLI-based client for direct blockmodeling, a set of techniques for finding latent structures in network data. A good introduction to blockmodeling is available on Wikipedia:
[https://en.wikipedia.org/wiki/Blockmodeling](https://en.wikipedia.org/wiki/Blockmodeling)

Socnet.se is developed in C#/.NET and can be compiled for multiple operating systems and architectures. See [INSTALL.md](INSTALL.md) for information on how to compile Socnet.se from source.

### Version 2.0
Version 2.0 is a complete rewrite of the Socnet.se client, with the same commands, syntax and output as earlier versions,
but with a much faster and more memory-efficient blockmodeling engine:
- Moving an actor between clusters updates the fit incrementally: only the blocks affected by the move are re-evaluated,
  and most ideal blocks are evaluated in constant time from aggregated block statistics.
- Searches run in parallel on all processor cores, with reproducible results for a given random seed (independent
  of the number of cores). The number of cores can be limited with the `threads` argument of `bminit` and `coreperi`,
  e.g. `bminit(net, bi, ljubljana, nordlund, threads = 2)`.
- The new command `system` shows information about the computer, e.g. the number of available processor cores.
- Socnet.se runs at below-normal priority, so that searches using all cores give way to other programs on the computer.
  Start the client with `--normalpriority` (or `-n`) to run at normal priority.
- The exhaustive search only evaluates each partition once when the blockimage is symmetric.
- The local searches ('localopt' and 'ljubljana') explore plateaus of equally good partitions in a bounded way,
  fixing the memory explosion (and crashes) of version 1.4 when many partitions have the same fit.

The code is organized in three projects: `Socnet.Core` (data structures, ideal blocks, searches), `Socnet.CLIconsole`
(the console application) and `Socnet.Tests` (tests). See [CONTRIBUTING.md](CONTRIBUTING.md) for how to add new ideal blocks.

Precompiled binaries and installation files for Windows, Linux, and MacOS are available on the project website:
[https://www.socnet.se](https://www.socnet.se)

Please check the `/example_data/` folder for example networks. This folder also contains the script `cli_script.txt` exemplifying how to conduct various kinds of direct blockmodeling analyses.

### Testing
The repository provides automated tests and scripts to test the core functionality of Socnet. Please see [TESTING.md](TESTING.md) for information on how to do these scripted tests. The automated tests are best done through your IDE.

### Authors and Funding
Socnet.se is developed and maintained by Carl Nordlund at the Institute for Analytical Sociology, Linköping University, Sweden, with contributions from José Luis Estévez, Kristian Gade Kjelmann,
Jesper Lindmarker, and Chandreyee Roy.
This project was supported by NordForsk through the funding to the Network Dynamics of Ethnic Integration (project number 105147). More information about this project can be found here:
[https://www.nordint.net](https://www.nordint.net)

### License
Socnet.se is released under the [MIT License](LICENSE.md), which allows anyone to use, modify, and redistribute the software for any purpose, including academic, research, or commercial use.

Since Socnet.se and its methods are publicly released and documented, this publication constitutes prior art, preventing others from patenting the same methods implemented in this software.
