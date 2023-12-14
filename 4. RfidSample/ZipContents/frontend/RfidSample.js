var timeoutID = null;
var getReaderInfo = true;
var updateTagsTable = false;

function callRfid(fn) {
	$.post('/api/application/RfidSample/rfid/' + fn,
		JSON.stringify({}), function (data) {
			if ('error' in data) {
				alert(data['error']);
			}
			if (timeoutID) {
				clearTimeout(timeoutID);
			}
			timeoutID = setTimeout(updateState, 50);
		}).fail(function () {
			alert('Failed to ' + fn + ' RFID reader');
		});
}

function callTags(fn) {
	$.post('/api/application/RfidSample/tags/' + fn + 'Stream',
		JSON.stringify({}), function (data) {
			if ('error' in data) {
				alert(data['error']);
			}
		}).fail(function () {
			alert('Failed to ' + fn + ' RFID inventory stream');
		});
}

function updateReaderInfo() {
	$.get('/api/application/RfidSample/rfid/readerinfo',
		function (data) {
			if ('error' in data) {
				alert(data['error']);
			} else {
				if ('name' in data) {
					document.getElementById('deviceName').innerHTML = data['name'];
				} else {
					document.getElementById('deviceName').innerHTML = '';
				}
				if ('fccId' in data) {
					document.getElementById('fccId').innerHTML = data['fccId'];
				} else {
					document.getElementById('fccId').innerHTML = '';
				}
				let ver = '';
				if ('swVerMajor' in data) {
					ver += data['swVerMajor'];
				}
				if ('swVerMinor' in data) {
					ver += '.' + data['swVerMinor'];
				}
				if ('devBuild' in data) {
					ver += ' ' + data['devBuild'];
				}
				document.getElementById('firmwareVersion').innerHTML = ver;
				getReaderInfo = false;
			}
		}).fail(function () {
			alert('Failed to get reader info');
			getReaderInfo = true;
		});
}

function updateState() {
	$.get('/api/application/RfidSample/rfid/connected',
		function (data) {
			if ('error' in data) {
				alert(data['error']);
			}
			let state = '';
			let cl = '';
			if ('connected' in data) {
				if (data['connected']) {
					state = 'Connected';
					cl = 'text-success';
					if (getReaderInfo) {
						updateReaderInfo();
					}
				} else {
					state = 'Disconnected';
					cl = 'text-danger';
					getReaderInfo = true;
					document.getElementById('deviceName').innerHTML = '';
					document.getElementById('fccId').innerHTML = '';
					document.getElementById('firmwareVersion').innerHTML = '';
				}
			} else {
				alert('Connected state missing in reply');
			}
			if ('connectError' in data) {
				state += ' (' + data['connectError'] + ')';
			}
			document.getElementById('rfidStatus').innerHTML = state;
			document.getElementById('rfidStatus').className = cl;
			timeoutID = setTimeout(updateState, 5000);
		}).fail(function () {
			alert('Failed to get RFID connected state');
			getReaderInfo = true;
		});

}

function tagsTable() {
	$('#tagsTable').DataTable({
		ajax: {
			url: '/api/application/RfidSample/inventory/get',
			dataSrc: function(json) {
				v = [];
				if ('tags' in json) {
					let tags = json['tags'];
					for (var id in tags) {
						let r = tags[id];
						v.push([id, r.epc, r.data, r.antennaId, r.rssi, r.phaseDiff, r.timesSeen]);
					}
				}
				if ('updateEnabled' in json && json['updateEnabled']) {
					updateTagsTable = true;
				}
				if (updateTagsTable) {
					setTimeout(() => { $("#tagsTable").DataTable().ajax.reload() }, 500);
				}
				return v;
			}
		},
		columnDefs: [
			{ "width": "20%", "targets": 0 },
		],
		paging: false,
		ordering: false,
		searching: false,
		info: false
	});
}

$(document).ready(function () {
	$('#connectButton').click(function () {
		callRfid('connect');
	});
	$('#disconnectButton').click(function () {
		callRfid('disconnect');
	});
	$('#startInventoryButton').click(function () {
		callTags('start');
		updateTagsTable = true;
		setTimeout(() => { $("#tagsTable").DataTable().ajax.reload() }, 100);
	});
	$('#stopInventoryButton').click(function () {
		callTags('stop');
		updateTagsTable = false;
	});
	timeoutID = setTimeout(updateState, 10);
	tagsTable();
});
