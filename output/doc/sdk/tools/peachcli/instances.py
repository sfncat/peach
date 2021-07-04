
'''
Configure peach instances for use with peachcli tool.

groups:

  Instances are always grouped. An instance can
  be part of multiple groups.  A group named master
  must exist.

master:
  
  A master group must exist with a single entry.

'''

INSTANCES = {
    "master": ["http://127.0.0.1:8888"],
    "all" : [
        "http://192.168.48.128:8888",
        "http://192.168.48.129:8888",
    ],
    "tls1" : [
        "http://192.168.48.128:8888",
    ],
    "tls11" : [
        "http://192.168.48.129:8888",
    ],
}

# end
