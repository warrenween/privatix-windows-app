﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.OpenVpn
{
    public static class OpenVpnConfig
    {
        public const string Text =
@"# Ansible managedclient
dev tun
proto {proto}
cipher AES-256-CBC
tls-client
remote {host} {port}
resolv-retry infinite
nobind
persist-key
ping-restart 10
route-method exe
management 127.0.0.1 {managment-port}

<ca>
-----BEGIN CERTIFICATE-----
MIIDQTCCAimgAwIBAgIJAMbWo8ChsiNaMA0GCSqGSIb3DQEBCwUAMBoxGDAWBgNV
BAMMD1ByaXZhdGl4LVZQTiBDQTAeFw0xNTEyMjIxMDEyMjhaFw0yNTEyMTkxMDEy
MjhaMBoxGDAWBgNVBAMMD1ByaXZhdGl4LVZQTiBDQTCCASIwDQYJKoZIhvcNAQEB
BQADggEPADCCAQoCggEBAMoPlSODj9C2ugsfNMAbxc5YBh2cg4FhsLb61rjHEPOW
atedwRedEpB3Fu0Qn1zGt2Snf8icRfoeeIsLWC8BfpfnJiDGz2bj68VT5ctoZDIW
syLyNvjBzWxqTHZNJPp9HQRXAlAw7t4mslxaV1/sVPvL0p5qd6DlERmoXOjdzSUJ
SHuscwOQbuaqgfskJETHPL9r54VBPOg1+QKt42SJ1FPK1IB1uR/oKIR98ynYMqgc
sQbdODZVHgi7UOkYay3FEbxBmXLX2hcOvaIPR0Q9Zd9C9XY+Tf9dTUZhPyoDomIo
94WbEYGnvKsfh31++Ju/kcdSwnpHoPwelCNAiR6Yrr8CAwEAAaOBiTCBhjAdBgNV
HQ4EFgQUsQ5CKCIZS8b8pCO5ULcshLHWy4swSgYDVR0jBEMwQYAUsQ5CKCIZS8b8
pCO5ULcshLHWy4uhHqQcMBoxGDAWBgNVBAMMD1ByaXZhdGl4LVZQTiBDQYIJAMbW
o8ChsiNaMAwGA1UdEwQFMAMBAf8wCwYDVR0PBAQDAgEGMA0GCSqGSIb3DQEBCwUA
A4IBAQDBSD01JaiCmaGb+GKMA3RvSLjm7lBh4ZkMcBvXKsU5CM7uNdfa95SpXt2b
JGnBbAbsH6SORI9h9Pi1p/fu4G0w6i1P6TVFGRrY5nnZFGKfzQxfe5Am2MzbS0Rc
28Ziudcz40WQrHWyLVJzXP84vnicILTjddGQJ+ePP4frE7+RvA1IO8+CFc+6kDcR
ioOFbkcvTg9FCjUNvqa1Ct2wXjEPv1as0vWu+XQw/J+9i9Qb7AiL5D0+xR9tppsl
GenjNRx1I61vcpLSVzUB7YItEn2Y5D7cYNOc5t0L1d+q5bXUNxtgxr2YvgMrse3T
7Pb/cNogz7RDgvVFEH7R/y1VvpzG
-----END CERTIFICATE-----
</ca>


# Enable compression on the VPN link. Don't enable this unless it is also
# enabled in the server config file.
comp-lzo

# Set log file verbosity.
verb 3
scramble obfuscate PRIVATIX
";
    }
}
