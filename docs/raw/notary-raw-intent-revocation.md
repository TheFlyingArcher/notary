# Certificate Revocation

## Overview

Certificate revocation is the process of invalidating a digital certificate before its scheduled expiration date. This is typically done when the certificate is compromised, the private key is lost, or the certificate holder's information changes. Revocation ensures that users and systems do not trust certificates that are no longer valid. I implemented the revocation process using the legacy CRL method from piecemeal documenation that I found online. However, it is incomplete, and I do not believe it is fully functional. I would like to request assistance in completing the implementation of the certificate revocation process. This is an area of certificate authorities that I am not familiar with, and I would like to ensure that the implementation is secure and effective.

## Revocation Methods

### Certificate Revocation List (CRL)

This is a legacy method for revoking certificates. A CRL is a list of revoked certificates published by the Certificate Authority (CA). Clients can download the CRL and check if a certificate is on the list before trusting it. However, this method has some drawbacks, such as the need for clients to download potentially large lists and the delay in revocation information propagation.

### OCSP (Online Certificate Status Protocol)

OCSP is a more modern method for checking the revocation status of a certificate. Instead of downloading a full list of revoked certificates, clients can query an OCSP responder to get the status of a specific certificate in real-time. This method is more efficient and provides up-to-date information, but it requires the CA to maintain an OCSP responder.

## Tasks

1. Evaluate the current implementation of the CRL method.
2. Identify which method would be best for the current Notary implementation.
3. Provide an implementation plan for the chosen revocation method, including any necessary changes to the existing codebase.
	1. If possible, would like to continue with using the Bouncy Castle library for the implementation, as it is already being used in the project. However, if there are better alternatives, please provide recommendations.
