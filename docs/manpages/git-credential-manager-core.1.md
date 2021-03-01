% Git-Credential-Manager-Core(1) Version 2.0.x | Git Credential Manager
% Microsoft Corporation
% March 2021

# NAME
git-credential-manager-core - the universal Git credential helper

# SYNOPSIS
**git-credential-manager-core** [---help] [---version] <*command*> [<*args*>]

# DESCRIPTION
Secure, cross-platform Git credential storage with authentication to GitHub,
Azure Repos, and other popular Git hosting services.

# COMMANDS
**get**
: Return a stored credential, or prompt for authentication.

**store**
: Store a credential.

**erase**
: Erase a stored credential.

**configure**
: Configure Git Credential Manager as the Git credential helper.

**unconfigure**
: Unconfigure Git Credential Manager as the Git credential helper.

# OPTIONS
**---help** **-h** **-?**
: Show help and usage information

**---version**
: Show version information

# EXAMPLES
**git-credential-manager-core get**
: Retrieve a stored credential or prompt for authentication from the user.

**git-credential-manager-core configure ---help**
: Display information about usage of the **configure** command.

**git-credential-manager-core configure ---system**
: Configure Git Credential Manager as the Git credential helper for the entire
system.

# EXIT VALUES
**0**
: Success

**Non-zero**
: Failure

# CONFIGURATION
Please see https://aka.ms/gcmcore-config for more information.

# ENVIRONMENT VARIABLES
Please see https://aka.ms/gcmcore-env for more information.

# BUGS
Please raise bugs at https://aka.ms/gcmcore-bug.

# COPYRIGHT
Copyright (c) Microsoft Corporation. All rights reserved.

# SEE ALSO
git(1), git-credential(1), git-config(1)
