
'''
Script to convert openssl console output into json/c# code.

The output from this script is used by thte TLS Check.

NOTE: Do not run openssl each time, instead use a master ciphers.txt file.
      Each platform will have a different openssl which could cause differences
	  in the output.  Also documentation needs to match.
'''

import re
import os
import json


def load_ciphers(fname):
    #  0xC0,0x30 - ECDHE-RSA-AES256-GCM-SHA384 TLSv1.2 Kx=ECDH     Au=RSA  Enc=AESGCM(256) Mac=AEAD
    
    with open(fname, 'r') as fd:
        data = fd.read()
    
    ciphers = re.findall('^\s*0x([^,\s]+),0x([^,\s]+)\s+-\s+([^\s]+)\s+([^\s]+)\s+Kx=([^\s]+)\s+Au=([^\s]+)\s+Enc=([^\s(]+)\(([^)]+)\)\s+Mac=([^\s]+)\s*$', data, flags=re.M)
    
    jc = []
    for cipher in ciphers:
        jd = {
            'id': '%s%s' % (cipher[0], cipher[1]),
            'cipher' : cipher[2],
            'version' : cipher[3].replace(".",""),
            'kx': cipher[4],
            'au':cipher[5],
            'enc':cipher[6],
            'enc_size':cipher[7],
            'mac':cipher[8]
            }
        
        jc.append(jd)
        
    return jc

jtop = {
    'all': load_ciphers('ciphers.txt'),
    'modern': load_ciphers('ciphers_modern.txt'),
    'intermediate':load_ciphers('ciphers_intermediate.txt')
}
    
#js = json.dumps(jtop, sort_keys=True, indent=4, separators=(',', ': '))

#with open('ciphers.json', 'wb') as fd:
#    fd.write(js)
    
#print js

for k in jtop:
	print "\n\n\n------- %s -------\n\n\n" % k

	for i in jtop[k]:
	
		print '''
			new TlsCipher
			{
				Authentication = "%(au)s",
				Cipher = "%(cipher)s",
				Encryption = "%(enc)s",
				EncryptionBitSize = %(enc_size)s,
				Id = "%(id)s",
				KeyExchange = "%(kx)s",
				Hmac = (TlsHMac)Enum.Parse(typeof(TlsHMac), "%(mac)s"),
				Version = (TlsVersion)Enum.Parse(typeof(TlsVersion), "%(version)s")
			},
''' % i

