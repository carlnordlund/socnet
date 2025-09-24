# Socnet.se client helper script
# =================================
# A set of useful functions for interacting with Socnet.se from R

# Uses the processx library so make sure to have that installed:
# install.packages("processx")

# To use the functions in this script, add the following line to your script
# (and adjust filepath as necessary):

# source("client_socnetse.R")

# Initiate the processx library
library(processx)

# Start the Socnet.se client process
# The binary is assumed to be in the same folder as the working directory
# The Socnet.se client is here run in silent mode with endmarker
.start_socnetse <- function(path = "socnet.exe") {
  if (exists(".socnetse_proc", envir=.GlobalEnv)) {
    stop("socnetse process already running.")
  }
  proc <- process$new(path, args=c("--endmarker","--silent"), stdin="|", stdout="|", stderr = "|")
  assign(".socnetse_proc", proc, envir=.GlobalEnv)
  invisible(proc)
}

# Stop the Socnet.se client process
.stop_socnetse <- function() {
  if (exists(".socnetse_proc", envir=.GlobalEnv)) {
    proc <- get(".socnetse_proc", envir=.GlobalEnv)
    if (proc$is_alive()) {
      proc$kill()
      message("socnetse process terminated.")
    }
    else {
      message("socnetse process is already not running.")
    }
    rm(".socnetse_proc", envir = .GlobalEnv)
  }
  else {
    message("No socnetse process found.")
  }
}

# Send a command to the Socnet.se client
# Returns any output
.send_command <- function(cmd) {
  proc <- get(".socnetse_proc", envir=.GlobalEnv)
  proc$write_input(paste0(cmd,"\n"))

  out <- character()
  repeat {
    new <- proc$read_output_lines()
    #print(new)
    if (length(new) > 0) {
      out <- c(out, new)
      if (any(endsWith(new, "__END__"))) {
        out <- out[out != "__END__"]  # drop the marker
        break
      }
    } else {
      Sys.sleep(0.01)  # small pause between polls
    }
  }
  if (length(out)>0)
    out
}

