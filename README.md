# Domain Forward Proxy
Domain forward proxy helps you setup cached proxy for secure internet web host

Problem Definition
1. SSL does not support caching on proxy server
2. Managing and trusting self generated certificates on proxy servers is complicated
3. Proxy server does not allow caching of only certain path
4. IIS ARR forward proxy does not support HTTPS

How to use
1. Setup an intranet website
2. Modify web.config and setup `remote-host` 
