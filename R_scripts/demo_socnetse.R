# Test script to demonstrate using Socnet.se from R

# Instructions (general usage pattern)
# 1. Load the 'client_socnet.se.R' file
# 2. Start the external process using the '.start_socnetse()' function (providing
#    the full path to the executable if in another folder).
# 3. Send CLI commands to Socnet.se using the '.send_command(...)' function,
#    assigning return values as applicable
# 4. Stop the external process using the '.stop_socnetse()' function



# Load file with base functions for communicating with Socnet.se
source("client_socnetse.R")

# Set the path to where the executable/binary file is
path_to_exe <- "C:/Users/pekpi/source/repos/socnet/bin/Debug/net8.0/socnet.exe"

# Start Socnet.se as an external process
.start_socnetse(path_to_exe)

# Set the random seed (if applicable)
# .send_command("randomseed(6031769)")

# Set the path to where your data files are
path_to_data <- "C:/Users/pekpi/Nextcloud/Work/Academic/Individual projects/Socnet.se JSS paper/R script example/example_data"

# Set the current working directory (here use )
.send_command(paste0("setwd(dir = '", path_to_data, "')"))

# Get the content of the current working directory
.send_command("dir()")

# Load the Little league TI example data
.send_command("loadmatrix(file = little_league_ti.txt, name = llti)")

# Create a 2x2 blockimage for structural equivalence, assign to 'bi2se'
.send_command("bi2se = blockimage(size = 2, pattern = com;nul)")

# Initialize an exhaustive search using hamming gof on the little league and 2x2 SE blockimage
.send_command("bminit(network = llti, blockimage = bi2se, searchtype = exhaustive, method = hamming)")

# Start the search
.send_command("bmstart()")

# Extract the resulting blockmodel matrix and store in variable name 'bm_llti_se2'
.send_command("bmextract(blockmodel = bm_llti_bi2se_0, type = matrix, outname = bm_llti_se2)")

# Extract the (single-blocked) blockimage for this optimal solution
.send_command("bmextract(blockmodel = bm_llti_bi2se_0, type = blockimage)")

# Extract the partition for this optimal solution
.send_command("bmextract(blockmodel = bm_llti_bi2se_0, type = partition)")

# Extract and store the goodness-of-fit string for this optimal solution, e.g. "29 (Hamming)"
gof_string <- .send_command("bmextract(blockmodel = bm_llti_bi2se_0, type = gof)")

# Obtain the actual goodness-of-fit value (removing the name), e.g. 29
gof <- as.numeric(sub("^(\\d+).*","\\1", gof_string))

# Store the blockmodel matrix for this solution in the file 'bm_llti_se2.txt'
# in the current working directory
.send_command("save(name = bm_llti_se2, file = bm_llti_se2.txt)")

# Stop the Socnet.se process
.stop_socnetse()
