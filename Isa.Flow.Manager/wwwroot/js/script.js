var timeoutValue = document.getElementById('timeoutValue').value;
var currentLastArticleId = 0;

function sendActorsAndQueuesStateRequest() {
    $.ajax({
        url: '/Home/GetState',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            if (data != null) {
                var newQueueCount = $('#newQueueCount');
                var declareNewQueue = $('#declareNewQueue');
                var newUpdatedQueue = $('#newUpdatedQueue');

                if (data.newCount != null) {    
                    declareNewQueue.css('display', 'none');

                    newUpdatedQueue.text(data.newAndUpdatedQueueName);
                    
                    newQueueCount.text(data.newCount);
                    newQueueCount.show();       
                }
                else {
                    newQueueCount.css('display', 'none');
                    declareNewQueue.show();

                    $(document).on('click', '#declareNewQueue', function () {
                        $('#declareNewQueueForm').modal('show');
                    });               
                }

                var deletedQueueCount = $('#deletedQueueCount');
                var declareDeletedQueue = $('#declareDeletedQueue');
                var deletedQueue = $('#deletedQueue');

                if (data.deletedCount != null) { 
                    declareDeletedQueue.css('display', 'none');

                    deletedQueue.text(data.deletedQueueName);

                    deletedQueueCount.text(data.deletedCount);
                    deletedQueueCount.show();
                }
                else {
                    deletedQueueCount.css('display', 'none');
                    declareDeletedQueue.show();

                    $(document).on('click', '#declareDeletedQueue', function () {
                        $('#declareDeletedQueueForm').modal('show');
                    });                
                }

                var extractorBlock = $('#extractorBlock');
                var buttonNew = $('#buttonNew');
                var buttonUpd = $('#buttonUpd');
                var buttonDel = $('#buttonDel');
                var openModal = $('#openModal');

                if (data.extractorStarted != null) {                  
                    extractorBlock.removeClass('isa-main-extractor-none');
                    extractorBlock.addClass('isa-main-extractor');

                    openModal.removeClass('isa-start-ex-download');
                    openModal.addClass('isa-start-ex');
                    openModal.removeAttr('disabled');
                    openModal.text('Запустить');

                    if (data.extractorStarted.lastArticleId != currentLastArticleId) {
                        currentLastArticleId = data.extractorStarted.lastArticleId;
                        $('#lastArticleIdState').text('LastArticleId: ' + currentLastArticleId);
                        addLabelAndInputForNew();
                    }

                    if (!data.extractorStarted.newState) {
                        buttonNew.removeClass('isa-main-progress-func-on');
                        buttonNew.addClass('isa-main-progress-func-off');
                        buttonNew.attr('disabled', true);
                        buttonNew.text('NEW Не активно');
                    }
                    else {
                        buttonNew.removeClass('isa-main-progress-func-off');
                        buttonNew.addClass('isa-main-progress-func-on');
                        buttonNew.removeAttr('disabled');
                        buttonNew.text('NEW Активно');
                    }

                    if (!data.extractorStarted.modifiedState) {
                        buttonUpd.removeClass('isa-main-progress-func-on');
                        buttonUpd.addClass('isa-main-progress-func-off');
                        buttonUpd.attr('disabled', true);
                        buttonUpd.text('UPDATED Не активно');
                    }
                    else {
                        buttonUpd.removeClass('isa-main-progress-func-off');
                        buttonUpd.addClass('isa-main-progress-func-on');
                        buttonUpd.removeAttr('disabled');
                        buttonUpd.text('UPDATED Активно');
                    }

                    if (!data.extractorStarted.deletedState) {
                        buttonDel.removeClass('isa-main-progress-func-on');
                        buttonDel.addClass('isa-main-progress-func-off');
                        buttonDel.attr('disabled', true);
                        buttonDel.text('DELETED Не активно');
                    }
                    else {
                        buttonDel.removeClass('isa-main-progress-func-off');
                        buttonDel.addClass('isa-main-progress-func-on');
                        buttonDel.removeAttr('disabled');
                        buttonDel.text('DELETED Активно');
                    }
                }
                else {
                    extractorBlock.removeClass('isa-main-extractor');
                    extractorBlock.addClass('isa-main-extractor-none');

                    buttonNew.removeClass('isa-main-progress-func-on');
                    buttonNew.addClass('isa-main-progress-func-off');
                    buttonNew.attr('disabled', true);
                    buttonNew.text('NEW Не активно');

                    buttonUpd.removeClass('isa-main-progress-func-on');
                    buttonUpd.addClass('isa-main-progress-func-off');
                    buttonUpd.attr('disabled', true);
                    buttonUpd.text('UPDATED Не активно');

                    buttonDel.removeClass('isa-main-progress-func-on');
                    buttonDel.addClass('isa-main-progress-func-off');
                    buttonDel.attr('disabled', true);
                    buttonDel.text('DELETED Не активно');

                    openModal.removeClass('isa-start-ex');
                    openModal.addClass('isa-start-ex-download');
                    openModal.attr('disabled', true);
                    openModal.text('Недоступно');
                }

                var indexerBlock = $('#indexerBlock');
                var buttonIndex = $('#buttonIndex');
                var indexSubmit = $('#indexSubmit');

                if (data.indexerStarted != null) {
                    indexerBlock.removeClass('isa-main-indexer-none');
                    indexerBlock.addClass('isa-main-indexer');

                    if (!data.indexerStarted) {
                        buttonIndex.removeClass('isa-main-index-on');
                        buttonIndex.addClass('isa-main-index-off');
                        buttonIndex.attr('disabled', true);
                        buttonIndex.text('Не активно');

                        indexSubmit.removeClass('isa-index-stop');
                        indexSubmit.removeClass('isa-index-start-download');
                        indexSubmit.addClass('isa-index-start');
                        indexSubmit.removeAttr('disabled');
                        indexSubmit.text('Запустить');
                    }
                    else {
                        buttonIndex.removeClass('isa-main-index-off');
                        buttonIndex.addClass('isa-main-index-on');
                        buttonIndex.removeAttr('disabled');
                        buttonIndex.text('Активно');

                        indexSubmit.removeClass('isa-index-start');
                        indexSubmit.removeClass('isa-index-start-download');
                        indexSubmit.addClass('isa-index-stop');
                        indexSubmit.attr('disabled', true);
                        indexSubmit.text('Запущено');
                    }
                }
                else {
                    indexerBlock.removeClass('isa-main-indexer');
                    indexerBlock.addClass('isa-main-indexer-none');

                    buttonIndex.removeClass('isa-main-index-on');
                    buttonIndex.addClass('isa-main-index-off');
                    buttonIndex.attr('disabled', true);
                    buttonIndex.text('Не активно');

                    indexSubmit.removeClass('isa-index-stop');
                    indexSubmit.removeClass('isa-index-start');
                    indexSubmit.addClass('isa-index-start-download');
                    indexSubmit.attr('disabled', true);
                    indexSubmit.text('Недоступно');
                }

                var tgCollectorBlock = $('#tgCollectorBlock');
                var buttonTgCollector = $('#buttonTgCollector');
                var tgCollectorSubmit = $('#tgCollectorSubmit');

                if (data.tgCollectorStarted != null) {
                    tgCollectorBlock.removeClass('isa-main-tgcollector-none');
                    tgCollectorBlock.addClass('isa-main-tgcollector');

                    if (!data.collectorStarted) {
                        buttonTgCollector.removeClass('isa-main-tgcollector-on');
                        buttonTgCollector.addClass('isa-main-tgcollector-off');
                        buttonTgCollector.attr('disabled', true);
                        buttonTgCollector.text('Не активно');

                        tgCollectorSubmit.removeClass('isa-tgcollector-stop');
                        tgCollectorSubmit.removeClass('isa-tgcollector-start-download');
                        tgCollectorSubmit.addClass('isa-tgcollector-start');
                        tgCollectorSubmit.removeAttr('disabled');
                        tgCollectorSubmit.text('Запустить');
                    }
                    else {
                        buttonTgCollector.removeClass('isa-main-tgcollector-off');
                        buttonTgCollector.addClass('isa-main-tgcollector-on');
                        buttonTgCollector.removeAttr('disabled');
                        buttonTgCollector.text('Активно');

                        tgCollectorSubmit.removeClass('isa-tgcollector-start');
                        tgCollectorSubmit.removeClass('isa-tgcollector-start-download');
                        tgCollectorSubmit.addClass('isa-tgcollector-stop');
                        tgCollectorSubmit.attr('disabled', true);
                        tgCollectorSubmit.text('Запущено');
                    }
                }
                else {
                    tgCollectorBlock.removeClass('isa-main-tgcollector');
                    tgCollectorBlock.addClass('isa-main-tgcollector-none');

                    buttonTgCollector.removeClass('isa-main-tgcollector-on');
                    buttonTgCollector.addClass('isa-main-tgcollector-off');
                    buttonTgCollector.attr('disabled', true);
                    buttonTgCollector.text('Не активно');

                    tgCollectorSubmit.removeClass('isa-tgcollector-stop');
                    tgCollectorSubmit.removeClass('isa-tgcollector-start');
                    tgCollectorSubmit.addClass('isa-tgcollector-start-download');
                    tgCollectorSubmit.attr('disabled', true);
                    tgCollectorSubmit.text('Недоступно');
                }

                var vkCollectorBlock = $('#vkCollectorBlock');
                var buttonVkCollector = $('#buttonVkCollector');
                var vkCollectorSubmit = $('#vkCollectorSubmit');

                if (data.vkCollectorStarted != null) {
                    vkCollectorBlock.removeClass('isa-main-vkcollector-none');
                    vkCollectorBlock.addClass('isa-main-vkcollector');

                    if (!data.vkCollectorStarted) {
                        buttonVkCollector.removeClass('isa-main-vkcollector-on');
                        buttonVkCollector.addClass('isa-main-vkcollector-off');
                        buttonVkCollector.attr('disabled', true);
                        buttonVkCollector.text('Не активно');

                        vkCollectorSubmit.removeClass('isa-vkcollector-stop');
                        vkCollectorSubmit.removeClass('isa-vkcollector-start-download');
                        vkCollectorSubmit.addClass('isa-vkcollector-start');
                        vkCollectorSubmit.removeAttr('disabled');
                        vkCollectorSubmit.text('Запустить');
                    }
                    else {
                        buttonVkCollector.removeClass('isa-main-vkcollector-off');
                        buttonVkCollector.addClass('isa-main-vkcollector-on');
                        buttonVkCollector.removeAttr('disabled');
                        buttonVkCollector.text('Активно');

                        vkCollectorSubmit.removeClass('isa-vkcollector-start');
                        vkCollectorSubmit.removeClass('isa-vkcollector-start-download');
                        vkCollectorSubmit.addClass('isa-vkcollector-stop');
                        vkCollectorSubmit.attr('disabled', true);
                        vkCollectorSubmit.text('Запущено');
                    }
                }
                else {
                    vkCollectorBlock.removeClass('isa-main-vkcollector');
                    vkCollectorBlock.addClass('isa-main-vkcollector-none');

                    buttonVkCollector.removeClass('isa-main-vkcollector-on');
                    buttonVkCollector.addClass('isa-main-vkcollector-off');
                    buttonVkCollector.attr('disabled', true);
                    buttonVkCollector.text('Не активно');

                    vkCollectorSubmit.removeClass('isa-vkcollector-stop');
                    vkCollectorSubmit.removeClass('isa-vkcollector-start');
                    vkCollectorSubmit.addClass('isa-vkcollector-start-download');
                    vkCollectorSubmit.attr('disabled', true);
                    vkCollectorSubmit.text('Недоступно');
                }
            }
            
            setTimeout(sendActorsAndQueuesStateRequest, timeoutValue);
        },
        error: function (error) {
            console.log(error);
        }
    });
}
setTimeout(sendActorsAndQueuesStateRequest, timeoutValue);

function startExtraction() {
    $.ajax({
        url: '/Home/StartExtraction',
        type: 'POST',
        data: $("#extractionForm").serialize(),
        success: function (data) {
            var button = null;

            if (data === '0') {
                button = $('#buttonNew');
                button.text('NEW Активно');
            }
            if (data == '1') {
                button = $('#buttonUpd');
                button.text('UPDATED Активно');
            }
            if (data == '2') {
                button = $('#buttonDel');
                button.text('DELETED Активно');
            }
            if (data == 'fromMoreThanTo') {
                window.alert("From не может быть больше To")
            }

            button.removeAttr('disabled');
            button.removeClass('isa-main-progress-func-off');
            button.addClass('isa-main-progress-func-on');

            $('#lastArticleForm').modal('hide');
        },
        error: function (error) {
            console.error(error);
        }
    });
}

function startIndex() {
    $.ajax({
        url: '/Home/StartIndex',
        type: 'POST',
        success: function (data) {
            if (data === 'indexing') {
                $('#buttonIndex').removeAttr('disabled');
                $('#buttonIndex').removeClass('isa-main-index-off');
                $('#buttonIndex').addClass('isa-main-index-on');
                $('#buttonIndex').text('Активно');

                $('#indexSubmit').removeClass('isa-index-start');
                $('#indexSubmit').addClass('isa-index-stop');
                $('#indexSubmit').attr('disabled', true);
                $('#indexSubmit').text('ЗАПУЩЕНО');
            }
        },
        error: function (error) {
            console.error(error);
        }
    });
}

function startTgCollector() {
    $.ajax({
        url: 'Home/StartTgCollection',
        type: 'POST',
        success: function (data) {
            if (data === 'phoneNumberRequested') {
                window.location.href = '/Home/ShowPhoneNumberForm'
            }
            if (data === 'verificationRequested') {
                window.location.href = "/Home/ShowVerificationForm"
            }
            if (data === 'started') {
                $('#buttonTgCollector').removeAttr('disabled');
                $('#buttonTgCollector').removeClass('isa-main-tgcollector-off');
                $('#buttonTgCollector').addClass('isa-main-tgcollector-on');
                $('#buttonTgCollector').text('Активно');

                $('#tgCollectorSubmit').removeClass('isa-tgcollector-start');
                $('#tgCollectorSubmit').addClass('isa-tgcollector-stop');
                $('#tgCollectorSubmit').attr('disabled', true);
                $('#tgCollectorSubmit').text('Запущено');
            }
            if (data.startsWith('error:')) {
                alert('Ошибка авторизации: ' + data.substring(6) + '\n\nПопробуйте еще раз\nПри неудачной попытке обратитесь к разработчику');
            }
        },
        error: function (error) {
            console.error(error);
        }
    });
}

function startVkCollector() {
    //$.ajax({
    //    url: 'Home/StartVkCollection',
    //    type: 'POST',
    //    success: function () {
    //        $('#buttonVkCollector').removeAttr('disabled');
    //        $('#buttonVkCollector').removeClass('isa-main-vkcollector-off');
    //        $('#buttonVkCollector').addClass('isa-main-vkcollector-on');
    //        $('#buttonVkCollector').text('Активно');

    //        $('#vkCollectorSubmit').removeClass('isa-vkcollector-start');
    //        $('#vkCollectorSubmit').addClass('isa-vkcollector-stop');
    //        $('#vkCollectorSubmit').attr('disabled', true);
    //        $('#vkCollectorSubmit').text('Запущено');
    //    },
    //    error: function (error) {
    //        console.error(error);
    //    }
    //});
   window.location.replace('/Home/StartVkCollection');
}

function stopVkCollection() {
    $.ajax({
        url: '/Home/StopVkCollection',
        type: 'POST',
        success: function () { },
        error: function (error) {
            console.error(error);
        }
    });
}

function stopTgCollection() {
    $.ajax({
        url: '/Home/StopTgCollection',
        type: 'POST',
        success: function () { },
        error: function (error) {
            console.error(error);
        }
    });
}

function stopIndex() {
    $.ajax({
        url: 'Home/StopIndex',
        type: 'POST',
        success: function (data) { },
        error: function (error) {
            console.error(error);
        }
    });
}

function stopExtractionFunc(value) {
    $.ajax({
        url: 'Home/StopExtraction',
        type: 'POST',
        data: { value: value },
        success: function (data) { },
        error: function (error) {
            console.error(error);
        }
    });
}

function declareNewQueue() {
    $.ajax({
        url: '/Home/QueueDeclare',
        type: 'POST',
        data: $('#newQueueForm').serialize(),
        success: function (data) {
            if (data != null) {
                window.alert(data);
            }
            else {
                var declareNewQueue = $('#declareNewQueue');
                declareNewQueue.css('display', 'none');

                var newQueueCount = $('#newQueueCount');
                newQueueCount.text('0');
                newQueueCount.show();

                $('#declareNewQueueForm').modal('hide');
            }
        },
        error: function (error) {
            console.error(error);
        }
    });
}

function declareDeletedQueue() {
    $.ajax({
        url: '/Home/QueueDeclare',
        type: 'POST',
        data: $('#deletedQueueForm').serialize(),
        success: function (data) {
            if (data != null) {
                window.alert(data);
            }
            else {
                var declareDeletedQueue = $('#declareDeletedQueue');
                declareDeletedQueue.css('display', 'none');

                var deletedQueueCount = $('#deletedQueueCount');
                deletedQueueCount.text('0');
                deletedQueueCount.show();

                $('#declareDeletedQueueForm').modal('hide');
            }
        },
        error: function (error) {
            console.error(error);
        }
    });
}
   
$('#openModal').click(function () {
    $('#lastArticleForm').modal('show');
});

$('#declareNewQueue').click(function () {
    $('#declareNewQueueForm').modal('show');
});

$('#declareDeletedQueue').click(function () {
    $('#declareDeletedQueueForm').modal('show');
});

function disableButton(button) {
    $(button).removeClass('isa-main-progress-func-on');
    $(button).addClass('isa-main-progress-func-off');
    $(button).attr('disabled', true);

    if (button.id == 'buttonNew') {
        stopExtractionFunc(0);

        $(button).text("NEW Не активно");

        option = $('#newOption');
        option.show();
    }
    if (button.id == 'buttonUpd') {
        stopExtractionFunc(1);

        $(button).text("UPDATED Не активно");

        option = $('#updOption');
        option.show();
    }
    if (button.id == 'buttonDel') {
        stopExtractionFunc(2);

        $(button).text("DELETED Не активно");

        option = $('#delOption');
        option.show();
    }    
    if (button.id == 'buttonIndex') {
        stopIndex();

        $(button).text("Не активно");
        $(button).attr('disabled');
        $(button).removeClass('isa-main-index-on');
        $(button).addClass('isa-main-index-off');

        $('#indexSubmit').removeAttr('disabled');
        $('#indexSubmit').removeClass('isa-index-stop');
        $('#indexSubmit').addClass('isa-index-start');
        $('#indexSubmit').text('Запустить');
    }
    if (button.id == 'buttonTgCollector') {
        stopTgCollection();

        $(button).text("Не активно");
        $(button).attr('disabled');
        $(button).removeClass('isa-main-tgcollector-on');
        $(button).addClass('isa-main-tgcollector-off');

        $('#tgCollectorSubmit').removeAttr('disabled');
        $('#tgCollectorSubmit').removeClass('isa-tgcollector-stop');
        $('#tgCollectorSubmit').addClass('isa-tgcollector-start');
        $('#tgCollectorSubmit').text('Запустить');
    }
    if (button.id == 'buttonVkCollector') {
        stopVkCollection();

        $(button).text("Не активно");
        $(button).attr('disabled');
        $(button).removeClass('isa-main-vkcollector-on');
        $(button).addClass('isa-main-vkcollector-off');

        $('#vkCollectorSubmit').removeAttr('disabled');
        $('#vkCollectorSubmit').removeClass('isa-vkcollector-stop');
        $('#vkCollectorSubmit').addClass('isa-vkcollector-start');
        $('#vkCollectorSubmit').text('Запустить');
    }
}

function addLabelAndInputForNew() {
    removeLabelAndInputForNew();
    removeLabelAndInputForReAndCl();

    var label = document.createElement("label");
    var input = document.createElement("input");

    label.setAttribute("for", "articleId");
    label.textContent = "Id";
    input.setAttribute("name", "ArticleId");
    input.setAttribute("id", "articleId");
    input.setAttribute("value", currentLastArticleId);

    var modalBody = document.getElementById("modalBody");
    modalBody.appendChild(label);
    modalBody.appendChild(input); 
}

function addLabelAndInputForReAndCl() {
    removeLabelAndInputForReAndCl();
    removeLabelAndInputForNew();

    var labelFrom = document.createElement("label");
    var inputFrom = document.createElement("input");
    var labelTo = document.createElement("label");
    var inputTo = document.createElement("input");

    labelFrom.setAttribute("for", "intervalFrom");
    labelFrom.textContent = "From";
    inputFrom.setAttribute("name", "From");
    inputFrom.setAttribute("id", "intervalFrom");
    inputFrom.setAttribute("value", "0");

    labelTo.setAttribute("for", "intervalTo");
    labelTo.textContent = "To";
    inputTo.setAttribute("name", "To");
    inputTo.setAttribute("id", "intervalTo");
    inputTo.setAttribute("value", "0");

    var modalBody = document.getElementById("modalBody");
    modalBody.appendChild(labelFrom);
    modalBody.appendChild(inputFrom);
    modalBody.appendChild(labelTo);
    modalBody.appendChild(inputTo);
}

function removeLabelAndInputForReAndCl() {
    var modalBody = document.getElementById("modalBody");

    var labelFrom = document.querySelector("label[for='intervalFrom']");
    var inputFrom = document.getElementById("intervalFrom");
    var labelTo = document.querySelector("label[for='intervalTo']");
    var inputTo = document.getElementById("intervalTo");

    if (labelFrom && inputFrom && labelTo && inputTo) {
        modalBody.removeChild(labelFrom);
        modalBody.removeChild(inputFrom);
        modalBody.removeChild(labelTo);
        modalBody.removeChild(inputTo);
    }
}

function removeLabelAndInputForNew() {
    var modalBody = document.getElementById("modalBody");
    var label = document.querySelector("label[for='articleId']");
    var input = document.getElementById("articleId");

    if (label && input) {
        modalBody.removeChild(label);
        modalBody.removeChild(input);
    }
}

document.getElementById("func").addEventListener("change", function () {
    var selectedValue = this.value;

    if (selectedValue === "0") {       
        addLabelAndInputForNew();
    }
    else if (selectedValue === "3" || selectedValue === "4") {       
        addLabelAndInputForReAndCl();
    }
    else {
        removeLabelAndInputForNew();
        removeLabelAndInputForReAndCl();
    }
});

var initialSelectedValue = document.getElementById("func").value;
if (initialSelectedValue === "0") {
    addLabelAndInputForNew();
}