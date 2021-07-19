#!/usr/bin/env python

import os
import argparse
from zipfile import ZipFile

if __name__ == "__main__":
	p = argparse.ArgumentParser(description='unzip')
	p.add_argument('-d', '--dir')
	p.add_argument('zip')

	args = p.parse_args()

	with ZipFile(args.zip, 'r') as z:
		z.extractall(args.dir)
