'use strict';
// Run with: node --test tests/js/*.test.cjs
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const app = relative => fs.readFileSync(path.join(root, 'src/LocalGPT', relative), 'utf8');

const kinds = ['spreadsheet', 'xls', 'csv', 'text', 'pdf', 'rtf', 'odt', 'html', 'txt', 'epub', 'doc', 'mht'];

test('7.7.8 the AI function accepts and advertises every PublisherStudio document kind', () => {
    const fn = app('Services/OneWire/CreatePublisherDocumentFunction.cs');
    const schema = JSON.parse(fn.slice(fn.indexOf('ParameterSchemaJson: """') + 'ParameterSchemaJson: """'.length, fn.indexOf('"""', fn.indexOf('ParameterSchemaJson: """') + 26)).trim());
    assert.deepEqual([...schema.properties.kind.enum].sort(), [...kinds].sort());
    const guard = fn.slice(fn.indexOf('if (kind is not ('), fn.indexOf('return organicSupport.Invalid("kind must be'));
    for (const kind of kinds) assert.ok(guard.includes(`"${kind}"`), `invoke guard accepts ${kind}`);
});

test('7.7.8 every returned kind is format-checked before it can be downloaded', () => {
    const state = app('Services/OneWire/OneWireStateServices.cs');
    const accept = state.slice(state.indexOf('if (kind is "spreadsheet"'), state.indexOf('&& !string.IsNullOrEmpty(encoded))'));
    for (const kind of kinds) assert.ok(accept.includes(`"${kind}"`), `result reader accepts ${kind}`);
    assert.match(state, /ValidateReturnedDocument\(kind, bytes\);\s*item\.DocumentKind = kind;/);
    const validator = state.slice(state.indexOf('private void ValidateReturnedDocument('), state.indexOf('/// <summary>Extracts a completed PublisherStudio document'));
    assert.match(validator, /"%PDF-"u8/);
    assert.match(validator, /@"\{\\rtf"u8/);
    assert.match(validator, /case "doc":\s*case "xls":[\s\S]*?0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1/);
    assert.match(validator, /case "html":\s*case "txt":\s*case "csv":[\s\S]*?throwOnInvalidBytes: true/);
    assert.match(validator, /"odt" => ReadMimetype\(\) == "application\/vnd\.oasis\.opendocument\.text"/);
    assert.match(validator, /"epub" => ReadMimetype\(\) == "application\/epub\+zip"/);
    assert.match(validator, /"spreadsheet" => [^\n]*"xl\/workbook\.xml"/);
    assert.match(validator, /"text" => [^\n]*"word\/document\.xml"/);
});

test('7.7.8 every kind downloads with its own file extension and a Chat label', () => {
    const controller = app('Controller/OneWireController.cs');
    const downloads = app('Components/Shared/ChatOfficeDownloads.razor');
    const extension = { spreadsheet: 'xlsx', text: 'docx' };
    for (const kind of kinds) {
        assert.match(controller, new RegExp(`"${kind}" => File\\(bytes, "[^"]+", "PublisherStudio\\.${extension[kind] ?? kind}"\\)`), `download for ${kind}`);
        if (kind !== 'text') assert.match(downloads, new RegExp(`"${kind}" => "Download `), `Chat label for ${kind}`);
    }
});
