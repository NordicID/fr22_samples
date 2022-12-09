#!/usr/bin/python3

import asyncio
import nid_rpc
import os
import logging
import json
import sys

class DemoServer:
    def __init__(self, appname):
        self.logger = logging.getLogger(__name__)
        d = os.path.abspath('frontend')
        if not os.path.isdir(d):
	        self.logger.error('frontend path "{}" not found'.format(d))
        self.rpc = nid_rpc.NidRpcPlugin('application', appname, d)
        self.rpc.filter_gmqtt_logs()

        self.stopEvent = asyncio.Event()

    async def run(self):
        await self.rpc.connect()
        await self.stopEvent.wait()

def main():
    srv = DemoServer(sys.argv[1])
    tasks = asyncio.gather(srv.run())
    loop = asyncio.get_event_loop()
    loop.run_until_complete(tasks)

if __name__ == '__main__':
    logging.basicConfig(level=logging.INFO)
    main()