# Prisma

Prisma is a simple web server for (Fast)CGI applications. It supports classical `cgi-bin/`/`.cgi` scripts, as well as (Fast)CGI compatible interpreters.

## Usage
Invoke with `prisma`. This will run Prisma for the current directory at port 8080.

Note that when specifying config options on the command line, they need to follow `--config`, or the config file will overwrite the provided values.

`CTRL-C` will stop Prisma.

### Options
```
Prisma web server 1.1.0.0
© 2021 Mainframe98

Usage: prisma

Options:
  -?, --help, -h             Display this help and exit.
  -v, --version              Show version information.
  -c, --config=VALUE         Path to the configuration file.
  -l, --log=VALUE            Path to the log file.
  -p, --port=VALUE           Port to listen to. Defaults to 8080.
  -d, --docroot=VALUE        Path to the document root (from which to serve the
                               files). Defaults to the working directory.
  -a, --allow-pathinfo       Allow path info.
      --enable-cgi-bin       Enable the cgi-bin/ directory.
      --enable-cgi-extension Treat all requests to scripts with a cgi extension
                               as cgi scripts.
      --listener-prefix=VALUE
                             Listen to these URI prefixes. This will overwrite
                               the default of 127.0.0.1 and the provided port,
                               and might require administrator privileges.
      --default-document=VALUE
                             Files to look for when a directory is requested.
  -j, --log-as-json          Log to the log file in the JSON format.
      --log-level=VALUE      Level of events to log. Supported levels: Verbose,
                               Debug, Information, Warning, Error, Fatal
```

### Installation
[Download an installer for Windows](https://github.com/Mainframe98/Prisma/releases/latest/download/PrismaSetup.exe), or [one of the binaries for linux and OSX](https://github.com/Mainframe98/Prisma/releases/latest).

See [build.md](Build.md) for instructions how to build Prisma.

## Configuration
Prisma is configured through a config file, which may be specified with `--config`.
It defines supported interpreters under `"Applications"`. A sample config file, with PHP as supported interpreter is available in `config.sample.json`.

### `RewriteRules`
Prisma can rewrite incoming requests. It uses the .NET regular expression engine, with the key specifying a regex that the request should match (path, excluding query) and the value the replacement. The replacement may include a query, any query included with the request will be correctly appended to it.

## Document handlers
### CGI invocation
Prisma complies with [RFC 3875](https://datatracker.ietf.org/doc/html/rfc3875), and provides the following additional environment variables:
 - `DOCUMENT_ROOT`
 - `REMOTE_PORT`
 - `REQUEST_URI`
 - `UNIQUE_ID`

Note that `REMOTE_HOST` is identical to `REMOTE_ADDR` in all circumstances.
   
#### Traditional `cgi-bin` invocations
Prisma supports `cgi-bin` directories. Requests directed to this directory will be treated as executables when `EnableCgiBin` is enabled in `config.json`, or the `--enable-cgi-bin` command line option is provided.

### FastCGI invocation
Prisma utilizes the [FastCGI for .NET](https://github.com/LukasBoersma/FastCGI) library for FastCGI communication.

FastCGI sockets may be defined as either an ip address (local or external) or as a Unix socket file.

Prisma can launch FastCGI applications at startup, if an application is specified under the "LaunchConfiguration" key. This makes manually starting the application unnecessary. Prisma will attempt to close the application when Prisma is stopped itself. Note that this is sometimes finicky, as programs might not be entirely ready before control is returned to Prisma.

### Default document handler
This handler will be used when no CGI/FastCGI handlers are defined for the given request. It will output the document as-is.

## About the name
Prisma is the Frisian word for prism, which in itself is a reference to the logo for the [Common Gateway Interface (CGI)](https://en.wikipedia.org/wiki/Common_Gateway_Interface).

# PrismaGUI
PrismaGUI is the WPF powered graphical user interface.
