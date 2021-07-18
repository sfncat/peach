mkdir -p pfce/peach
echo "export INT_PATH=`pwd`/pfce" >> ~/.bashrc
echo "export PATH=$INT_PATH:$PATH" >>~/.bashrc
echo "alias runpit='docker exec -it peach_ir /peach/peach'" >> ~/.bashrc
source ~/.bashrc