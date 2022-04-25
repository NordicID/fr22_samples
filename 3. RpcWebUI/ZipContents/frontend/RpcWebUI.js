function beep(time) {
	let payload = { 'time': time };
	$.post('/api/application/RpcWebUI/beep',
		JSON.stringify(payload), function (data) {
			if ('error' in data) {
				alert(data['error']);
			}
		}).fail(function () {
			alert(data);
		});
}

$(document).ready(function () {
	$('#shortBeepButton').click(function () {
		beep(250);
	});
	$('#longBeepButton').click(function () {
		beep(2000);
	});
});
