var path = "temp";
var filename = "";

function doExport() {
	fr22BackendGet(`/api/application/MultipartDemo/${path}/export`)
		.done( () => window.location = fr22Address('/static/application/MultipartDemo/file') );
}

function doDelete() {
	fr22BackendGet(`/api/application/MultipartDemo/${path}/delete`)
		.done( function() {
			$('#filename').text("No file imported");
			fr22ShowToast('success', "Deleted file");
		});
}

function importState(state) {
	fr22BackendGet(`/api/application/MultipartDemo/${path}/state?state=` + state.key + "&long_polling=15000")
		.done(function(data) {
			if ("state" in data) {
				$('#importState').text(data["state"].replace(/_/g, ' '));
				$('#importState').css('textTransform', 'capitalize');
				state.key = data["state"];
				setTimeout(importState, 10, state);
			} else {
				$('#importState').text("Unknown error");
				$('#importState').css('textTransform', 'none');
				setTimeout(importState, 2000, state);
			}
		})
		.fail(function(jqXHR, textStatus, errorThrown) {
			if (textStatus == 'error') {
				$('#importState').text(errorThrown);
				$('#importState').css('textTransform', 'none');
			}
			setTimeout(importState, 2000, state);
		});
}

$(document).ready(function() {
	$("#savetodisk").click( function() {
		path = $(this).is(':checked') ? "disk" : "temp";
	});
	$("#exportBut").click( () => doExport() );
	$("#deleteBut").click( () => doDelete() );
	setTimeout(importState, 10, { key: ""});
	$("#importForm").attr("action", fr22Address(`/api/application/MultipartDemo/${path}`)).submit( e => e.preventDefault() );
	$('#importBut').on('click', function () {
		var fd = new FormData(document.getElementById('importForm'));
		if (!($('#importFile').val().trim())) {
			fr22ShowToast('error', "Please select file before starting import");
			return;
		}
		filename = $('#upload-file-info')[0].innerText;
		$("#savetodisk").prop('disabled', true);
		$("#importBut").prop('disabled', true);
		$.ajax({
			url: fr22Address(`/api/application/MultipartDemo/${path}`),
			type: 'POST',
			method: 'POST',
			data: fd,
			cache: false,
			contentType: false,
			processData: false,
			success: function(result) {
				if ("error" in result) {
					fr22ShowToast('error', "Import failed: " + JSON.stringify(result));
					$('.progress-bar').width('0%');
				} else {
					fr22ShowToast('success', "Import completed");
					$('#filename').text(filename);
				}
			},
			error: function(result) {
				fr22ShowToast('error', "Import failed");
				$('.progress-bar').width('0%');
			},
			complete: function(result) {
				$('#importFile').val("");
				$('#upload-file-info').html("");
				$("#savetodisk").prop('disabled', false);
				$("#importBut").prop('disabled', false);
			},
			xhr: function () {
				var myXhr = new window.XMLHttpRequest();
				if (myXhr.upload) {
					myXhr.upload.addEventListener('progress', function (evt) {
						if (evt.lengthComputable && evt.total > 0) {
							var percentComplete = evt.loaded / evt.total;
							percentComplete = parseInt(percentComplete * 100);
							$('.progress-bar').width(percentComplete+'%');
							$('.progress-bar').html(percentComplete+'%');
						}
					}, false);
				}
				return myXhr;
			}
		});
	});
});
