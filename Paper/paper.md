---
title: "Socnet.se: An Open-Source C# Console Application for Direct Blockmodeling"
tags:
- direct blockmodeling
- role analysis
- network analysis
- C#
authors:
- name: Carl Nordlund
  orcid: "0000-0001-5057-1985"
  corresponding: true
  affiliation: 1
- name: José Luis Estévez
  orcid: "0000-0001-9044-7612"
  affiliation: "2, 3"
- name: Kristian Gade Kjelmann
  orcid: "0000-0001-7994-735X"
  affiliation: 4
- name: Jesper Lindmarker
  orcid: "0000-0002-2561-7651"
  affiliation: 1
- name: Chandreyee Roy
  orcid: "0000-0002-5517-1056"
  affiliation: 5
affiliations:
- name: Linköping University, Sweden
  index: 1
- name: University of Helsinki, Finland
  index: 2
- name: Population Research Institute, Väestöliitto, Finland
  index: 3
- name: Aalborg University, Denmark
  index: 4
- name: Aalto University, Finland
  index: 5
date: "16 December 2025"
bibliography: paper.bib
---

# Summary

The `Socnet.se` client is an open-source, cross-platform console application for direct blockmodeling of one-mode network data. Developed in C#, it unifies multiple blockmodeling approaches—structural, regular, and generalized equivalence—into a single tool, supporting both binary and valued networks through classical Hamming distance and correlation-based goodness-of-fit measures. The software includes specialized functionality for detecting various core-periphery structures and provides integrated isomorphism detection for blockmodels. `Socnet.se` operates via a simple scripting language and can be used as a standalone command-line tool, through script files, or as an external process called from R, Python, or similar environments. Given sufficient knowledge in C#, Socnet.se can easily be modified and extended with additional types of equivalences, ideal blocks, goodness-of-fit measures and search heuristics, as well as additional data structures if needed, and the code may also be scavenged for the further development of other existing or future software solutions for direct blockmodeling. `Socnet.se` is available as pre-compiled binaries for Windows, Linux, and macOS at [www.socnet.se](https://www.socnet.se), and can also be compiled directly from source.

# Statement of Need

Building on the foundational ideas of Harrison White [@lorrain_structural_1971] on how to capture the notion of social roles in relational terms, direct blockmodeling is a pivotal method in network analysis for identifying how actors occupy similar structural positions in a network and how these positions relate to one another [@Doreian2005]. Unlike community detection or core-periphery identification, blockmodeling maintains neutrality toward the structures that might emerge, making it valuable for exploratory analysis of social, organizational, and other relational data. Starting off with a meaningful notion of actor equivalence, direct blockmodeling involves the systematic identification and grouping of actors that share such relational similarities, while simultaneously mapping out the overarching relational patterns within and between such subsets of equivalent actors (aka ‘positions’). A well-fitted blockmodel, characterized by emerging blocks of relations within and between positions mirroring certain ideal counterparts, enables the effective reduction of a potentially intricate and complex network to a blockimage, capturing its underlying functional anatomy [@Nordlund2020], allocating the actors to these various roles according to such relational similarities.

The current software ecosystem for direct blockmodeling is fragmented across multiple tools, each implementing different subsets of methods:

- **Pajek** [@Batagelj2004] is the most established tool but only supports binary networks and is closed-source Windows software
- **The blockmodeling R package** [@Ziberna2007] implements valued blockmodeling with novel ideal blocks but lacks correlation-based approaches
- **Specialized tools** for core-periphery detection [@Borgatti2000] and various extensions to this exist as separate implementations in various languages

This fragmentation means researchers must learn multiple tools, convert data between formats, and often cannot replicate certain approaches. Moreover, recent methodological advances, such as dichotomization-free valued blockmodeling analysis [@Nordlund2020], power-relational core-periphery models [@Nordlund2018], and extensions with p-cores [@Estevez2025], have so far lacked any accessible implementations.

`Socnet.se` addresses these gaps by providing:

1. **Unified approach**: Provides both binary and valued blockmodeling, using either Hamming distances or correlation-based measures
2. **Comprehensive ideal blocks**: Implements all classical blocks (null, complete, regular, row/column-regular, row/column-functional) plus several specialized blocks for core-periphery structures
3. **Multiple equivalence types**: Supports structural, regular, and generalized equivalence in a single tool
4. **Open and extensible**: MIT-licensed with object-oriented architecture designed for adding new ideal blocks, goodness-of-fit measures, and search algorithms
5. **Cross-platform**: Available as pre-compiled binaries for Windows, Linux, and macOS, as well as build scripts for respective platform

Table 1 below provides a comparison of features available in Pajek, Žiberna's blockmodeling R package, and Socnet.se.

| Feature | Pajek | blockmodeling R package | Socnet.se |
|-----------|---------|--------------------|-----------|
| Equivalence types | Structural, regular, generalized | Structural, regular, generalized, homogeneity | Structural, regular, generalized |
| Binary networks | Yes (Hamming only) | Yes (Hamming + homogeneity) | Yes (Hamming + correlation) |
| Valued networks | No (must dichotomize) | Yes (novel valued blocks) | Yes (correlation-based) |
| Borgatti-Everett-style Core-periphery models | No | No | Yes (multiple variants) |
| Open source | No | Yes (GPL-2/3) | Yes (MIT) |
| Platforms | Windows only | Win/Linux/macOS | Win/Linux/macOS |

Table 1: Comparison of direct blockmodeling software features for one-mode networks.

The target audience includes social network researchers, computational social scientists, and anyone analyzing relational data who needs flexible, theory-driven methods for uncovering latent structural patterns.

Socnet.se has so far been used in @Estevez2025, providing the implementations for the extensions to the traditional Borgatti-Everett approach to identifying core-periphery structures, and its functionality for partially constrained blockimages is being applied in ongoing research on world-system structures. Socnet.se has also been used in social network analysis course modules in the computational social science Masters program at Linköping University, Sweden.

# Key Features

`Socnet.se` provides comprehensive functionality for direct blockmodeling:

**Blockmodeling Approaches**:

- Structural equivalence: Actors with identical tie patterns
- Regular equivalence: Actors with ties to equivalent others
- Generalized equivalence: Flexible combinations of ideal blocks
- Pre-specified structure search: Test networks against hypothesized models
- Pre-specified partitions: Test models against hypothesized partitions

**Ideal Block Types**:

- Classical blocks: null, complete, regular, row/column-regular, row/column-functional
- Density blocks: exact density, minimum density, Ucinet-style density [@Borgatti2000]
- Core-periphery blocks: p-cores, peripheral dependency and/or core dominance
- Do-not-care blocks: Allow any pattern without penalty

**Goodness-of-Fit Measures**:

- Hamming distance (inconsistency counts) for binary networks [@Doreian2005]
- Weighted point-biserial correlation [@Nordlund2020] for valued networks (also applicable to binary)

**Search Algorithms**:

- Exhaustive search for small networks
- `localopt`: Local optimization with optional actor switching
- `ljubljana`: Semi-stochastic depth/width hybrid search
- Configurable parameters: number of random tests for starting partitions, number of restarts, iteration limits, minimum cluster sizes, timeout settings

**Core-Periphery Detection**:

- Built-in `coreperi()` function with shortcut access to multiple core-periphery models
- Classical Borgatti-Everett model [@Borgatti2000]
- Power-relational varieties [@Nordlund2018]: peripheral dependency and/or core dominance
- Extensions with p-cores and adjusted density blocks [@Estevez2025]

**Additional Functionality**:

- Automatic detection and filtering of isomorphic solutions and blockimages
- Partially constrained blockimages (mixing single or multiple ideal blocks in a blockimage)
- Incremental expansion of blockimage templates based on previous optimal findings (using the `biextend()` command; see online documentation)
- Data transformations: dichotomization, symmetrization, rescaling
- Density table generation for partitions
- Matrix and edgelist file formats
- Integrated file handling and data management

Full documentation on how to use the above features, all available Socnet CLI commands, and a comprehensive quick-start guide are available at https://socnet.se/.

# Research impact
Developed within the scope of the Network Dynamics of Ethnic integration project, a large Nordic research project funded by the NordForsk program on interdisciplinary research,
the Socnet.se client was first introduced in a paper in *Social Networks* [@Estevez2025], where we propose and implement extensions to the classical Borgatti-Everett approach to identify and
quantify core-periphery structures. Socnet.se has also been used on course modules in Social network analysis within the MSc program in Computational Social Science at Linköping university, Sweden,
as well as the Research School in Computational Social Science. In ongoing research on the structure of the contemporary world-system, the features of partially constrained blockimages are proven to
be particularly useful in an ongoing study on semiperipheral patterns.

# Usage Example

Below exemplifies a typical workflow for doing structural equivalence blockmodeling
of the Little League TI data [@fine_boys], as provided in the `/example_data` folder,
when working on the command-line interface of the `Socnet.se` client. A full
specification of the scripting language used by `Socnet.se` is available on the
project website as well a quick-start user guide exemplifying
various use patterns. There is also an extensively commented replication script
(`cli_script.txt`) available in the `/example_data` folder of the repository.

```
# Make sure that the Socnet.se working directory is where the data file is:
> setwd("[path_to_example_data_folder]")

# Load network data
> loadmatrix(little_league_ti.txt, name=llti)
Stored structure 'llti_actors' (Actorset)
Stored structure 'llti' (Matrix)
Loading data structure: OK

# Create a 4-position blockimage with complete and null blocks (i.e. structural
# equivalence)
> bi4se = blockimage(size = 4, type = structural)
Stored structure 'bi4se' (BlockImage)

# Initialize search using ljubljana algorithm with Hamming distance
> bminit(network=llti, blockimage=bi4se, searchtype=ljubljana, method=hamming)
Initializing search...
Network: llti
Method: hamming
nbrrestarts: 50
maxiterations: 100
nbrrandomstart: 50
minnbrbetter: 5
Search heuristic: ljubljana
Blockimage: bi4se
minclustersize: 1
maxtime: 300000ms (timeout active)
Initialization seems to have gone ok!

# Execute search
> bmstart()
Execution time (ms):229
Nbr tests done:25853
Stored structure 'bm_llti_bi4se_0' (BlockModel)
Goodness-of-fit (1st BlockModel): 20 (hamming)

# In this example, using the Little League TI data, we arrive at a solution
# named 'bm_llti_bi4se_0'

# View results
> bmview(blockmodel = bm_llti_bi4se_0)
Blockmodel:
+----------------+
|\  XX|   X| |   |      0_John_6
|X\   |X   | |   |      0_Jerry_10
| X\  |X   |X|   |      0_Darrin_11
|XX \ |X   | |   |      0_Ben_12
|X   \|   X| |   |      0_Arnie_13
+----------------+
|     |\XXX| |   |      1_Ron__1
|  X  |X\X | |   |      1_Frank_3
|     |XX\ |X|   |      1_Boyd_4
|     |XXX\| |   |      1_Tim__5
+----------------+
|  X  |XX  |\|   |      2_Tom__2
+----------------+
|     |    |X|\XX|      3_Jeff_7
|     |    |X|X\X|      3_Jay__8
|     |    |X|XX\|      3_Sandy_9
+----------------+
Blockimage:
        P0      P1      P2      P3
P0      nul     nul     nul     nul
P1      nul     com     nul     nul
P2      nul     nul     nul     nul
P3      nul     nul     com     com
Goodness-of-fit: 20 (hamming)

# Extract blockmodel matrix, naming it 'bm_llti_se4'
> bmextract(bm_llti_bi4se_0, type = matrix, outname = bm_llti_se4)
Stored structure 'bm_llti_se4_actors' (Actorset)
Stored structure 'bm_llti_se4' (Matrix)
Stored structure 'bm_llti_se4_ideal' (Matrix)

# Save the blockmodel matrix to file
> save(name = bm_llti_se4, file = bm_llti_se4.txt)
Matrix 'bm_llti_se4' saved: bm_llti_se4.txt
```

For core-periphery detection, the process is simplified:

```
> loadmatrix(zachary.txt)
> coreperi(zachary, ljubljana)
> bmview(bm_zachary_cp_0)

# For non-optional arguments, argument names can be omitted if stated in the
# specified order.
```

Additional examples including valued network analysis and power-relational core-periphery detection are provided in the comprehensive documentation at https://socnet.se/. The `/example_data/` folder also contains an extensively commented script - `cli_script.txt` - that describes how typical analytical workflows look like.

# Software Design
Developed explicitly with compactness and self-sufficiency in mind, with no other dependencies than the standard libraries of .NET 8.0, the `Socnet.se` client builds on standard object-oriented principles to allow for future extensions. Specifically, the codebase has been prepared with three types of extensions in mind: implementing new ideal blocks, new goodness-of-fit measures, and new search heuristics. While a short introduction to extending `Socnet.se` follows below, more detailed
technical instructions are provided in the `CONTRIBUTING.txt` file that is available in the `Socnet.se` repository.

## Ideal blocks
Each ideal block is implemented as a separate class that inherits from a shared abstract class (`_Block.cs`), the latter specifying the necessary properties and virtual methods of all ideal blocks. To add a new ideal block, a new class is created (in the `/DataLibrary/Blocks/` folder) that implements the block-specific properties and goodness-of-fit method(s). To make a new ideal block available when constructing blockimages, the ideal block is finally registered in the `BlockmodelingConstants.cs` class. An extensively commented template (`_exampleBlock.cs`) is provided in the `/DataLibrary/Blocks/` folder to facilitate this process.

## Goodness-of-fit measures
`Socnet.se` currently includes two measures (Hamming and weighted correlation-based), but additional measures can be implemented as well. Each measure is defined as a method in
`Blockmodeling.cs` with a specific method signature: a new goodness-of-fit method should thus follow the same signature as the `Blockmodeling.binaryHamming(...)` method. The
new measure must then be implemented for all ideal block classes that should support it, which also must be declared in `BlockmodelingConstants.cs` and added as an option in the
`bminit()` command and the `InitializeSearch()` method.

## Search heuristics
New heuristics and algorithms for finding optimal partitions can also be implemented in `Socnet.se`. Each search approach is implemented as a separate method in `Blockmodeling.cs` and
initiated as a method delegate. All these search methods must be registered in `BlockmodelingConstants.cs` and exposed as an option in the `bminit()` command.

# Integration with Other Software

`Socnet.se` can be integrated into existing workflows:

- **R integration**: The GitHub repository includes `client_socnetse.R`, a library of wrapper functions for calling `Socnet.se` from R. Make sure that the Socnet client is first installed properly. See the `demo_socnetse.R` file for a demonstration on how to use the R wrapper.
- **Python integration**: Also possible to invoke as an external process from Python
- **Script files**: Batch processing via script files containing Socnet.se commands
- **Standalone use**: Direct command-line interaction for exploratory analysis

The software reads and writes standard text-based formats (tab-separated matrices, edgelists), simplifying compatibility with common network analysis tools like igraph, networkx, Pajek, and Ucinet.

# Related Software

`Socnet.se` complements existing blockmodeling tools:

- **Pajek** [@Batagelj2004]: Comprehensive network analysis software with binary blockmodeling; `Socnet.se` extends this with valued network support and correlation-based methods
- **blockmodeling R package** [@Ziberna2007]: Implements valued blockmodeling with novel ideal blocks; `Socnet.se` provides correlation-based alternatives and core-periphery specialization
- **Core-periphery tools**: Various implementations [@Kojaku2018; @Schoch2023] focus specifically on the traditional core-periphery model of Borgatti-Everett [@Borgatti2000]; `Socnet.se` integrates this within a broader blockmodeling framework and provides a variety of extended core-periphery models

# Acknowledgments

This research was supported by NordForsk through funding to the Network Dynamics of Ethnic Integration (project number 105147). We thank colleagues in the research project and students in the Masters programme in Computational Social Science at Linköping University, Sweden, for valuable feedback.

# AI Usage Disclosure
No generative AI tools were used in the development of the software code. AI assistance (`claude.ai`) was partly used for proofreading and improving readability of the manuscript text.

# References





