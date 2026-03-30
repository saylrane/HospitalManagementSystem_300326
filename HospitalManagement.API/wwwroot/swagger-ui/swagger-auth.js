(function () {
    function addTokenInput() {
        var header = document.getElementsByClassName('swagger-ui')[0];
        if (!header) return;

        var container = document.createElement('div');
        container.style.padding = '10px';
        container.style.textAlign = 'right';

        var input = document.createElement('input');
        input.type = 'text';
        input.placeholder = 'Bearer {token}';
        input.style.width = '400px';
        input.id = 'swagger-auth-token';

        var btn = document.createElement('button');
        btn.innerText = 'Set Token';
        btn.style.marginLeft = '8px';
        btn.onclick = function () {
            var token = document.getElementById('swagger-auth-token').value;
            if (!token) return;
            // Set default Authorization header for Swagger UI requests
            window.ui.getConfigs().requestInterceptor = function (req) {
                req.headers['Authorization'] = token;
                return req;
            };
            alert('Authorization header set');
        };

        container.appendChild(input);
        container.appendChild(btn);

        // Insert at top of swagger UI
        var topbar = document.querySelector('.topbar') || header;
        topbar.parentNode.insertBefore(container, topbar.nextSibling);
    }

    window.addEventListener('load', function () {
        setTimeout(addTokenInput, 1000);
    });
})();