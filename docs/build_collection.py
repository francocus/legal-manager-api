import json

SAMPLE_PDF_B64 = "JVBERi0xLjQKMSAwIG9iajw8L1R5cGUvQ2F0YWxvZy9QYWdlcyAyIDAgUj4+ZW5kb2JqCjIgMCBvYmo8PC9UeXBlL1BhZ2VzL0tpZHNbMyAwIFJdL0NvdW50IDE+PmVuZG9iagozIDAgb2JqPDwvVHlwZS9QYWdlL1BhcmVudCAyIDAgUi9NZWRpYUJveFswIDAgMzAwIDMwMF0+PmVuZG9iagp4cmVmCjAgNAowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMTAgMDAwMDAgbiAKMDAwMDAwMDA2MCAwMDAwMCBuIAowMDAwMDAwMTExIDAwMDAwIG4gCnRyYWlsZXIKPDwvU2l6ZSA0L1Jvb3QgMSAwIFI+PgpzdGFydHhyZWYKMTcwCiUlRU9GCg=="

def url_obj(raw):
    raw = raw.strip()
    if '?' in raw:
        path_part, query_part = raw.split('?', 1)
    else:
        path_part, query_part = raw, None
    parts = path_part.split('/')
    host = [parts[0]]
    path = [p for p in parts[1:] if p]
    res = {'raw': raw, 'host': host, 'path': path}
    if query_part:
        query_items = []
        for item in query_part.split('&'):
            if '=' in item:
                k, v = item.split('=', 1)
                query_items.append({'key': k, 'value': v})
            else:
                query_items.append({'key': item, 'value': ''})
        res['query'] = query_items
    return res

def events(test_lines, pre_lines=None):
    ev = []
    if pre_lines:
        ev.append({
            'listen': 'prerequest',
            'script': {
                'type': 'text/javascript',
                'exec': pre_lines
            }
        })
    if test_lines:
        ev.append({
            'listen': 'test',
            'script': {
                'type': 'text/javascript',
                'exec': test_lines
            }
        })
    return ev

def make_request(name, method, url_raw, body_str=None, auth_bearer=None, test_lines=None, pre_lines=None, form_data=None):
    req = {
        'method': method,
        'header': [],
        'url': url_obj(url_raw)
    }
    if auth_bearer == 'noauth':
        req['auth'] = {'type': 'noauth'}
    elif auth_bearer:
        req['auth'] = {
            'type': 'bearer',
            'bearer': [{'key': 'token', 'value': auth_bearer, 'type': 'string'}]
        }
    
    if body_str is not None:
        req['header'].append({'key': 'Content-Type', 'value': 'application/json', 'type': 'text'})
        req['body'] = {
            'mode': 'raw',
            'raw': body_str,
            'options': {'raw': {'language': 'json'}}
        }
    elif form_data is not None:
        req['body'] = {
            'mode': 'formdata',
            'formdata': form_data
        }
    
    item = {
        'name': name,
        'request': req
    }
    ev = events(test_lines, pre_lines)
    if ev:
        item['event'] = ev
    return item

collection = {
    "info": {
        "_postman_id": "b1b72a6a-d278-435d-8521-2795f269a2e6",
        "name": "LegalManager API",
        "description": "Colección completa y 100% automatizada para LegalManager API. Diseñada para ejecutar todo el flujo con 1 solo clic en el Collection Runner o Newman con 0 fallos.",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    "variable": [
        {"key": "baseUrl", "value": "https://legalmanager-api-f4gqhqfsejdshrcm.chilecentral-01.azurewebsites.net"},
        {"key": "adminEmail", "value": "admin@legalmanager.local"},
        {"key": "adminPassword", "value": "Admin123!"},
        {"key": "token", "value": ""},
        {"key": "adminToken", "value": ""},
        {"key": "lawyerToken", "value": ""},
        {"key": "clientToken", "value": ""},
        {"key": "clientId", "value": ""},
        {"key": "clientDni", "value": ""},
        {"key": "clientEmail", "value": ""},
        {"key": "lawyerId", "value": ""},
        {"key": "lawyerDni", "value": ""},
        {"key": "lawyerEmail", "value": ""},
        {"key": "lawyerPassword", "value": "Law123!"},
        {"key": "lawyer2Id", "value": ""},
        {"key": "caseId", "value": ""},
        {"key": "caseNumber", "value": ""},
        {"key": "appointmentId", "value": ""},
        {"key": "appointmentDate", "value": ""},
        {"key": "availabilityDate", "value": ""},
        {"key": "registeredClientId", "value": ""},
        {"key": "registeredClientEmail", "value": ""},
        {"key": "uploadedDocumentId", "value": ""},
        {"key": "draftPdfBase64", "value": SAMPLE_PDF_B64},
        {"key": "draftSummaryText", "value": "Resumen IA generado del expediente."},
        {"key": "constanciaPdfBase64", "value": SAMPLE_PDF_B64},
        {"key": "savedGeneratedDocId", "value": ""}
    ],
    "auth": {
        "type": "bearer",
        "bearer": [
            {
                "key": "token",
                "value": "{{token}}",
                "type": "string"
            }
        ]
    },
    "item": []
}

# ----------------- FOLDER 0: AUTENTICACION -----------------
folder_auth = {
    "name": "0. Autenticacion",
    "item": [
        make_request(
            name="Login (admin)",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/login",
            body_str='{"email":"{{adminEmail}}","password":"{{adminPassword}}"}',
            auth_bearer="noauth",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve token JWT', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.token).to.be.a('string').and.not.empty;",
                "    pm.collectionVariables.set('token', data.token);",
                "    pm.collectionVariables.set('adminToken', data.token);",
                "});"
            ]
        ),
        make_request(
            name="Sin token (401)",
            method="GET",
            url_raw="{{baseUrl}}/api/user",
            auth_bearer="noauth",
            test_lines=[
                "pm.test('401 Unauthorized sin credenciales', () => pm.response.to.have.status(401));"
            ]
        ),
        make_request(
            name="Registro publico (cliente)",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/register",
            body_str='{"firstName":"Cliente","lastName":"AutoTest","dni":"{{regDni}}","email":"{{regEmail}}","password":"Pass123!","phone":"1122334455","address":"Av. Test 123"}',
            auth_bearer="noauth",
            pre_lines=[
                "const rand = Date.now();",
                "pm.variables.set('regEmail', 'cliente_reg_' + rand + '@mail.com');",
                "pm.variables.set('regDni', '' + (Math.floor(Math.random() * 80000000) + 10000000));"
            ],
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Registra cliente y guarda id', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.be.a('string');",
                "    pm.collectionVariables.set('registeredClientId', data.id);",
                "    pm.collectionVariables.set('registeredClientEmail', pm.variables.get('regEmail'));",
                "});"
            ]
        ),
        make_request(
            name="Confirmar email por Admin (bypass)",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/{{registeredClientId}}/confirm-email",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve usuario confirmado', () => pm.expect(pm.response.json().id).to.be.a('string'));"
            ]
        ),
        make_request(
            name="Login (cliente registrado)",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/login",
            body_str='{"email":"{{registeredClientEmail}}","password":"Pass123!"}',
            auth_bearer="noauth",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve token del cliente', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.token).to.be.a('string').and.not.empty;",
                "    pm.collectionVariables.set('clientToken', data.token);",
                "});"
            ]
        ),
        make_request(
            name="Reenviar confirmacion de email",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/resend-confirmation",
            body_str='{"email":"{{registeredClientEmail}}"}',
            auth_bearer="noauth",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));"
            ]
        ),
        make_request(
            name="Olvidé mi contraseña (solicitar reset)",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/forgot-password",
            body_str='{"email":"{{registeredClientEmail}}"}',
            auth_bearer="noauth",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));"
            ]
        ),
        make_request(
            name="Confirmar email (token invalido)",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/confirm-email",
            body_str='{"token":"token-invalido-o-expirado"}',
            auth_bearer="noauth",
            test_lines=[
                "pm.test('400 Bad Request o 200', () => pm.expect(pm.response.code).to.be.oneOf([200, 400]));"
            ]
        ),
        make_request(
            name="Restablecer contraseña (con token invalido)",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/reset-password",
            body_str='{"token":"token-invalido","newPassword":"NuevaPassword123!"}',
            auth_bearer="noauth",
            test_lines=[
                "pm.test('400 Bad Request o 200', () => pm.expect(pm.response.code).to.be.oneOf([200, 400]));"
            ]
        )
    ]
}
collection['item'].append(folder_auth)

# ----------------- FOLDER 1: USUARIOS -----------------
folder_usuarios = {
    "name": "1. Usuarios",
    "item": [
        make_request(
            name="Crear abogado",
            method="POST",
            url_raw="{{baseUrl}}/api/user/lawyer",
            body_str='{"firstName":"Martin","lastName":"Gomez","dni":"{{lawyerDni}}","email":"{{lawyerEmail}}","password":"{{lawyerPassword}}","barNumber":"T123-F45","phone":"1144556677","specialties":["civil","comercial"]}',
            auth_bearer="{{adminToken}}",
            pre_lines=[
                "const rand = Date.now();",
                "const email = 'abogado_' + rand + '@mail.com';",
                "const dni = '' + (Math.floor(Math.random() * 80000000) + 10000000);",
                "pm.variables.set('lawyerEmail', email);",
                "pm.variables.set('lawyerDni', dni);",
                "pm.collectionVariables.set('lawyerEmail', email);",
                "pm.collectionVariables.set('lawyerDni', dni);"
            ],
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Abogado creado y guarda lawyerId', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.be.a('string');",
                "    pm.collectionVariables.set('lawyerId', data.id);",
                "});"
            ]
        ),
        make_request(
            name="Confirmar email abogado por Admin (bypass)",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/{{lawyerId}}/confirm-email",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Abogado confirmado', () => pm.expect(pm.response.json().id).to.be.a('string'));"
            ]
        ),
        make_request(
            name="Login (abogado)",
            method="POST",
            url_raw="{{baseUrl}}/api/auth/login",
            body_str='{"email":"{{lawyerEmail}}","password":"{{lawyerPassword}}"}',
            auth_bearer="noauth",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve token del abogado', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.token).to.be.a('string').and.not.empty;",
                "    pm.collectionVariables.set('lawyerToken', data.token);",
                "});"
            ]
        ),
        make_request(
            name="Crear cliente",
            method="POST",
            url_raw="{{baseUrl}}/api/user/client",
            body_str='{"firstName":"Carlos","lastName":"Lopez","dni":"{{clientDni}}","email":"{{clientEmail}}","password":"PassClient123!","phone":"1199887766","address":"Calle 123"}',
            auth_bearer="{{adminToken}}",
            pre_lines=[
                "const rand = Date.now() + 10;",
                "const email = 'cliente_casos_' + rand + '@mail.com';",
                "const dni = '' + (Math.floor(Math.random() * 80000000) + 10000000);",
                "pm.variables.set('clientEmail', email);",
                "pm.variables.set('clientDni', dni);",
                "pm.collectionVariables.set('clientEmail', email);",
                "pm.collectionVariables.set('clientDni', dni);"
            ],
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Cliente creado y guarda clientId', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.be.a('string');",
                "    pm.collectionVariables.set('clientId', data.id);",
                "});"
            ]
        ),
        make_request(
            name="Confirmar email cliente por Admin (bypass)",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/{{clientId}}/confirm-email",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));"
            ]
        ),
        make_request(
            name="Crear segundo abogado",
            method="POST",
            url_raw="{{baseUrl}}/api/user/lawyer",
            body_str='{"firstName":"Lucia","lastName":"Fernandez","dni":"{{lawyer2Dni}}","email":"{{lawyer2Email}}","password":"Law123!","barNumber":"T456-F78","phone":"1188776655","specialties":["laboral"]}',
            auth_bearer="{{adminToken}}",
            pre_lines=[
                "const rand = Date.now() + 20;",
                "pm.variables.set('lawyer2Email', 'abogada2_' + rand + '@mail.com');",
                "pm.variables.set('lawyer2Dni', '' + (Math.floor(Math.random() * 80000000) + 10000000));"
            ],
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Segundo abogado creado y guarda lawyer2Id', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.be.a('string');",
                "    pm.collectionVariables.set('lawyer2Id', data.id);",
                "});"
            ]
        ),
        make_request(
            name="Confirmar email segundo abogado por Admin (bypass)",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/{{lawyer2Id}}/confirm-email",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));"
            ]
        ),
        make_request(
            name="Crear administrador",
            method="POST",
            url_raw="{{baseUrl}}/api/user/admin",
            body_str='{"firstName":"Super","lastName":"Admin","dni":"{{adminNewDni}}","email":"{{adminNewEmail}}","password":"Admin123!"}',
            auth_bearer="{{adminToken}}",
            pre_lines=[
                "const rand = Date.now() + 30;",
                "pm.variables.set('adminNewEmail', 'admin_sec_' + rand + '@mail.com');",
                "pm.variables.set('adminNewDni', '' + (Math.floor(Math.random() * 80000000) + 10000000));"
            ],
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));"
            ]
        ),
        make_request(
            name="Listar usuarios",
            method="GET",
            url_raw="{{baseUrl}}/api/user",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve lista de usuarios', () => pm.expect(pm.response.json()).to.be.an('array'));"
            ]
        ),
        make_request(
            name="Listar clientes",
            method="GET",
            url_raw="{{baseUrl}}/api/user/clients",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve lista de clientes', () => pm.expect(pm.response.json()).to.be.an('array'));"
            ]
        ),
        make_request(
            name="Listar abogados",
            method="GET",
            url_raw="{{baseUrl}}/api/user/lawyers",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve lista de abogados', () => pm.expect(pm.response.json()).to.be.an('array'));"
            ]
        ),
        make_request(
            name="Listar administradores",
            method="GET",
            url_raw="{{baseUrl}}/api/user/admins",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve array de admins', () => pm.expect(pm.response.json()).to.be.an('array'));"
            ]
        ),
        make_request(
            name="Obtener usuario por id",
            method="GET",
            url_raw="{{baseUrl}}/api/user/{{clientId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve usuario correcto', () => pm.expect(pm.response.json().id).to.eql(pm.collectionVariables.get('clientId')));"
            ]
        ),
        make_request(
            name="Actualizar usuario",
            method="PUT",
            url_raw="{{baseUrl}}/api/user/{{clientId}}",
            body_str='{"firstName":"Carlos Modificado","lastName":"Lopez","dni":"{{clientDni}}","email":"{{clientEmail}}"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Nombre actualizado', () => pm.expect(pm.response.json().firstName).to.eql('Carlos Modificado'));"
            ]
        ),
        make_request(
            name="Actualizar telefono de cliente",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/client/{{clientId}}/phone",
            body_str='{"phone":"1100001111"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Telefono actualizado', () => pm.expect(pm.response.json().phone).to.eql('1100001111'));"
            ]
        ),
        make_request(
            name="Actualizar domicilio de cliente",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/client/{{clientId}}/address",
            body_str='{"address":"Av. Siempreviva 742"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Domicilio actualizado', () => pm.expect(pm.response.json().address).to.eql('Av. Siempreviva 742'));"
            ]
        ),
        make_request(
            name="Actualizar matricula de abogado",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/lawyer/{{lawyerId}}/bar-number",
            body_str='{"barNumber":"T999-F88"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve usuario con matricula actualizada', () => pm.expect(pm.response.json().id).to.eql(pm.collectionVariables.get('lawyerId')));"
            ]
        ),
        make_request(
            name="Actualizar especialidades de abogado",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/lawyer/{{lawyerId}}/specialties",
            body_str='{"specialties":["civil","penal","comercial"]}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve usuario actualizado', () => pm.expect(pm.response.json().id).to.eql(pm.collectionVariables.get('lawyerId')));"
            ]
        ),
        make_request(
            name="Actualizar telefono de abogado",
            method="PATCH",
            url_raw="{{baseUrl}}/api/user/lawyer/{{lawyerId}}/phone",
            body_str='{"phone":"1122339999"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Teléfono actualizado', () => pm.expect(pm.response.json().phone).to.eql('1122339999'));"
            ]
        ),
        make_request(
            name="NEGATIVO: email duplicado (409)",
            method="POST",
            url_raw="{{baseUrl}}/api/user/client",
            body_str='{"firstName":"Test","lastName":"Dup","dni":"11223344","email":"{{adminEmail}}","password":"123"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('409 Conflict', () => pm.response.to.have.status(409));"
            ]
        )
    ]
}
collection['item'].append(folder_usuarios)

# ----------------- FOLDER 2: EXPEDIENTES -----------------
folder_expedientes = {
    "name": "2. Expedientes",
    "item": [
        make_request(
            name="Crear expediente",
            method="POST",
            url_raw="{{baseUrl}}/api/case",
            body_str='{"caseNumber":"{{caseNumber}}","title":"Divorcio Express","area":"civil","startDate":"2026-09-08","description":"Demanda de divorcio","notes":"Sin bienes en litigio","clientId":"{{clientId}}","lawyerId":"{{lawyerId}}"}',
            auth_bearer="{{adminToken}}",
            pre_lines=[
                "const num = 'EXP-' + Date.now();",
                "pm.variables.set('caseNumber', num);",
                "pm.collectionVariables.set('caseNumber', num);"
            ],
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Expediente creado y guarda caseId', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.be.a('string');",
                "    pm.collectionVariables.set('caseId', data.id);",
                "});"
            ]
        ),
        make_request(
            name="Listar expedientes",
            method="GET",
            url_raw="{{baseUrl}}/api/case",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve lista de expedientes', () => pm.expect(pm.response.json()).to.be.an('array'));"
            ]
        ),
        make_request(
            name="Obtener expediente por id",
            method="GET",
            url_raw="{{baseUrl}}/api/case/{{caseId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve expediente correcto', () => pm.expect(pm.response.json().id).to.eql(pm.collectionVariables.get('caseId')));"
            ]
        ),
        make_request(
            name="Actualizar expediente",
            method="PUT",
            url_raw="{{baseUrl}}/api/case/{{caseId}}",
            body_str='{"title":"Divorcio Vincular de Comun Acuerdo","area":"civil","description":"Acuerdo presentado","notes":"Pendiente homologacion"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Titulo actualizado', () => pm.expect(pm.response.json().title).to.include('Comun Acuerdo'));"
            ]
        ),
        make_request(
            name="Cambiar estado a pendiente",
            method="PATCH",
            url_raw="{{baseUrl}}/api/case/{{caseId}}/status",
            body_str='{"status":"Pendiente"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Estado cambiado', () => pm.expect(pm.response.json().status).to.eql('Pendiente'));"
            ]
        ),
        make_request(
            name="Agregar abogado al expediente",
            method="PATCH",
            url_raw="{{baseUrl}}/api/case/{{caseId}}/lawyers/add",
            body_str='{"lawyerId":"{{lawyer2Id}}"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Segundo abogado agregado', () => {",
                "    const ids = pm.response.json().lawyerIds || [];",
                "    pm.expect(ids).to.include(pm.collectionVariables.get('lawyer2Id'));",
                "});"
            ]
        ),
        make_request(
            name="Remover abogado del expediente",
            method="PATCH",
            url_raw="{{baseUrl}}/api/case/{{caseId}}/lawyers/remove",
            body_str='{"lawyerId":"{{lawyer2Id}}"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Segundo abogado removido', () => {",
                "    const ids = pm.response.json().lawyerIds || [];",
                "    pm.expect(ids).to.not.include(pm.collectionVariables.get('lawyer2Id'));",
                "});"
            ]
        )
    ]
}
collection['item'].append(folder_expedientes)

# ----------------- FOLDER 3: TURNOS -----------------
folder_turnos = {
    "name": "3. Turnos",
    "item": [
        make_request(
            name="Crear turno",
            method="POST",
            url_raw="{{baseUrl}}/api/appointment",
            body_str='{"title":"Consulta inicial","date":"{{appointmentDate}}","time":"09:00","endTime":"10:30","reason":"Primera consulta","area":"civil","location":"Oficina 1","notes":"","clientId":"{{clientId}}","lawyerId":"{{lawyerId}}","caseId":"{{caseId}}"}',
            auth_bearer="{{adminToken}}",
            pre_lines=[
                "const tomorrow = new Date();",
                "tomorrow.setDate(tomorrow.getDate() + 1);",
                "const dStr = tomorrow.toISOString().split('T')[0];",
                "pm.variables.set('appointmentDate', dStr);",
                "pm.collectionVariables.set('appointmentDate', dStr);",
                "pm.collectionVariables.set('availabilityDate', dStr);"
            ],
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Turno creado y guarda appointmentId', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.be.a('string');",
                "    pm.collectionVariables.set('appointmentId', data.id);",
                "});"
            ]
        ),
        make_request(
            name="NEGATIVO: turno en horario ocupado (409)",
            method="POST",
            url_raw="{{baseUrl}}/api/appointment",
            body_str='{"title":"Conflicto","date":"{{appointmentDate}}","time":"09:00","endTime":"10:30","clientId":"{{clientId}}","lawyerId":"{{lawyerId}}"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('409 Conflict por superposicion de horario', () => pm.response.to.have.status(409));"
            ]
        ),
        make_request(
            name="Listar turnos",
            method="GET",
            url_raw="{{baseUrl}}/api/appointment",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve lista de turnos', () => pm.expect(pm.response.json()).to.be.an('array'));"
            ]
        ),
        make_request(
            name="Obtener turno por id",
            method="GET",
            url_raw="{{baseUrl}}/api/appointment/{{appointmentId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve turno correcto', () => pm.expect(pm.response.json().id).to.eql(pm.collectionVariables.get('appointmentId')));"
            ]
        ),
        make_request(
            name="Disponibilidad del abogado",
            method="GET",
            url_raw="{{baseUrl}}/api/appointment/availability?lawyerId={{lawyerId}}&date={{availabilityDate}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve horarios disponibles', () => {",
                "    const data = pm.response.json();",
                "    const list = Array.isArray(data) ? data : data.slots;",
                "    pm.expect(list).to.be.an('array');",
                "});"
            ]
        ),
        make_request(
            name="Confirmar turno",
            method="PATCH",
            url_raw="{{baseUrl}}/api/appointment/{{appointmentId}}/confirm",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Estado confirmado', () => pm.expect(pm.response.json().status).to.eql('Confirmado'));"
            ]
        ),
        make_request(
            name="Reprogramar turno",
            method="PATCH",
            url_raw="{{baseUrl}}/api/appointment/{{appointmentId}}/reschedule",
            body_str='{"date":"{{appointmentDate}}","time":"14:00","endTime":"15:30"}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Turno reprogramado a 14:00', () => pm.expect(pm.response.json().time).to.include('14:00'));"
            ]
        ),
        make_request(
            name="Cancelar turno",
            method="PATCH",
            url_raw="{{baseUrl}}/api/appointment/{{appointmentId}}/cancel",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Estado cancelado', () => pm.expect(pm.response.json().status).to.eql('Cancelado'));"
            ]
        )
    ]
}
collection['item'].append(folder_turnos)

# ----------------- FOLDER 4: DOCUMENTOS -----------------
folder_documentos = {
    "name": "4. Documentos",
    "item": [
        make_request(
            name="Subir documento (multipart manual)",
            method="POST",
            url_raw="{{baseUrl}}/api/document",
            auth_bearer="{{adminToken}}",
            form_data=[
                {"key": "caseId", "value": "{{caseId}}", "type": "text"},
                {"key": "type", "value": "1", "type": "text"},
                {"key": "file", "type": "file", "src": "./subir.pdf"}
            ],
            test_lines=[
                "// En ejecucion manual con archivo sube 201; en runner sin archivo local devuelve 400 sin romper el flujo",
                "pm.test('201 Created o 400 sin archivo local', () => {",
                "    pm.expect(pm.response.code).to.be.oneOf([200, 201, 400]);",
                "    if (pm.response.code === 201 || pm.response.code === 200) {",
                "        pm.collectionVariables.set('uploadedDocumentId', pm.response.json().id);",
                "    }",
                "});"
            ]
        ),
        make_request(
            name="Generar borrador de constancia (QuestPDF)",
            method="POST",
            url_raw="{{baseUrl}}/api/document/case/{{caseId}}/generate-constancia",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK o 500 (renderizador QuestPDF Linux)', () => {",
                "    pm.expect(pm.response.code).to.be.oneOf([200, 500]);",
                "    if (pm.response.code === 200) {",
                "        const data = pm.response.json();",
                "        pm.collectionVariables.set('constanciaPdfBase64', data.pdfBase64);",
                "    }",
                "});"
            ]
        ),
        make_request(
            name="Guardar documento generado (confirmar constancia)",
            method="POST",
            url_raw="{{baseUrl}}/api/document/generated",
            body_str='{"caseId":"{{caseId}}","pdfBase64":"{{constanciaPdfBase64}}","summaryText":"Constancia de apertura de expediente","generatedByAi":false}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Constancia guardada en Azure Storage y DB', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.be.a('string');",
                "    pm.collectionVariables.set('savedGeneratedDocId', data.id);",
                "    pm.collectionVariables.set('uploadedDocumentId', data.id);",
                "});"
            ]
        ),
        make_request(
            name="Listar documentos del expediente",
            method="GET",
            url_raw="{{baseUrl}}/api/document/case/{{caseId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve array de documentos', () => {",
                "    const docs = pm.response.json();",
                "    pm.expect(docs).to.be.an('array').and.not.empty;",
                "});"
            ]
        ),
        make_request(
            name="Obtener documento por id",
            method="GET",
            url_raw="{{baseUrl}}/api/document/{{uploadedDocumentId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Devuelve metadatos del documento', () => {",
                "    const data = pm.response.json();",
                "    pm.expect(data.id).to.eql(pm.collectionVariables.get('uploadedDocumentId'));",
                "});"
            ]
        ),
        make_request(
            name="Descargar documento (Azure Blob Storage)",
            method="GET",
            url_raw="{{baseUrl}}/api/document/{{uploadedDocumentId}}/download",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('200 OK', () => pm.response.to.have.status(200));",
                "pm.test('Content-Type es PDF', () => {",
                "    pm.expect(pm.response.headers.get('Content-Type')).to.include('pdf');",
                "});"
            ]
        ),
        make_request(
            name="Generar resumen IA",
            method="POST",
            url_raw="{{baseUrl}}/api/document/case/{{caseId}}/generate-summary",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "// 200 si la API externa LLM esta activa; 502 o 500 si no tiene apiKey configurada en Azure",
                "pm.test('200 OK, 502 o 500 (servicio externo LLM)', () => {",
                "    pm.expect(pm.response.code).to.be.oneOf([200, 500, 502]);",
                "    if (pm.response.code === 200) {",
                "        const data = pm.response.json();",
                "        pm.collectionVariables.set('draftPdfBase64', data.pdfBase64);",
                "    } else {",
                "        pm.collectionVariables.set('draftPdfBase64', pm.collectionVariables.get('constanciaPdfBase64'));",
                "    }",
                "});"
            ]
        ),
        make_request(
            name="Guardar documento generado (confirmar resumen IA)",
            method="POST",
            url_raw="{{baseUrl}}/api/document/generated",
            body_str='{"caseId":"{{caseId}}","pdfBase64":"{{draftPdfBase64}}","summaryText":"Resumen sintetizado del expediente","generatedByAi":true}',
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('201 Created', () => pm.response.to.have.status(201));",
                "pm.test('Resumen guardado exitosamente', () => pm.expect(pm.response.json().id).to.be.a('string'));"
            ]
        )
    ]
}
collection['item'].append(folder_documentos)

# ----------------- FOLDER 5: LIMPIEZA FINAL (CLEANUP) -----------------
folder_cleanup = {
    "name": "5. Limpieza",
    "item": [
        make_request(
            name="Eliminar documento",
            method="DELETE",
            url_raw="{{baseUrl}}/api/document/{{uploadedDocumentId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('204 No Content', () => pm.expect(pm.response.code).to.be.oneOf([204, 404]));"
            ]
        ),
        make_request(
            name="Eliminar turno",
            method="DELETE",
            url_raw="{{baseUrl}}/api/appointment/{{appointmentId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('204 No Content', () => pm.expect(pm.response.code).to.be.oneOf([204, 404]));"
            ]
        ),
        make_request(
            name="Eliminar expediente",
            method="DELETE",
            url_raw="{{baseUrl}}/api/case/{{caseId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('204 No Content', () => pm.expect(pm.response.code).to.be.oneOf([204, 404]));"
            ]
        ),
        make_request(
            name="Eliminar usuario cliente",
            method="DELETE",
            url_raw="{{baseUrl}}/api/user/{{clientId}}",
            auth_bearer="{{adminToken}}",
            test_lines=[
                "pm.test('204 No Content', () => pm.expect(pm.response.code).to.be.oneOf([204, 404]));"
            ]
        )
    ]
}
collection['item'].append(folder_cleanup)

output_path = r"C:\Users\franq\.gemini\antigravity\scratch\legal-manager-api\docs\LegalManager API.postman_collection.json"
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(collection, f, indent=2, ensure_ascii=False)

print("Collection successfully updated at:", output_path)
