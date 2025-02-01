const mongoose = require('mongoose');
const args = require('args-parser')(process.argv);
const { Schema } = mongoose;

mongoose.set('strictQuery', false);

if (!args.server && !args.database) {
    console.error('Server (--server) and database (--database) name are required');
    return 1;
}

let credentials = '';
let port = 27017;
if (args.username && args.password) {
    credentials = `${args.username}:${args.password}@`;
}

if (args.port) {
    port = args.port;
}

const asymmetricKeySchema = new Schema({
    slug: { type: String, required: true, index: true },
    enc_prv_key: { type: String, require: true, index: false },
    alg: { type: String, enum: ['RSA', 'EllipticCurve'], required: true },
    curve: {
        type: String,
        enum: ['P256', 'P384', 'P521', 'P256K'],
        required: false
    },
    key_len: { type: Number, required: false, index: true },
    name: { type: String, required: true, index: true },
    na: { type: Date, default: Date.now },
    nb: { type: Date, default: Date.now },
    active: { type: Boolean, required: true },
    created: { type: Date, default: Date.now },
    createdBy: { type: String, required: true },
    updated: Date,
    updatedBy: String
})

const certificateSchema = new Schema({
    slug: { type: String, required: true, index: true },
    data: { type: String, required: true, index: true },
    iss: {
        cn: String,
        c: String,
        l: String,
        o: String,
        ou: String,
        s: String
    },
    is_ca: { type: Boolean, required: true, index: true },
    keyUsage: { type: Number, required: true },
    key_slug: { type: Number, required: false },
    name: { type: String, required: true, index: true },
    nb: { type: Date, default: Date.now },
    na: { type: Date, default: Date.now },
    rd: Date,
    sn: { type: String, required: true, index: true },
    sigAlg: { type: String, required: true },
    sub: {
        cn: String,
        c: String,
        l: String,
        o: String,
        ou: String,
        s: String
    },
    san: [{
        kind: Number,
        name: String
    }],
    thumb: { type: String, required: true, index: true },
    active: { type: Boolean, required: true },
    created: { type: Date, default: Date.now },
    createdBy: { type: String, required: true },
    updated: Date,
    updatedBy: String
});

const certificateAuthoritySchema = new Schema({
    slug: { type: String, required: true },
    alg: { type: String, enum: ['RSA', 'EllipticCurve'], required: true },
    cert_slug: { type: String, required: true },
    curve: {
        type: String,
        enum: ['P256', 'P384', 'P521', 'P256K'],
        required: false
    },

    dn: {
        cn: String,
        c: String,
        l: String,
        o: String,
        ou: String,
        s: String
    },
    is_issuer: Boolean,
    issuing_dn: {
        cn: String,
        c: String,
        l: String,
        o: String,
        ou: String,
        s: String
    },
    key_len: { type: Number, required: false, index: true },
    name: { type: String, required: true },
    parent_slug: { type: String, required: false },
    active: { type: Boolean, required: true },
    created: { type: Date, default: Date.now },
    createdBy: { type: String, required: true },
    updated: Date,
    updatedBy: String
});

const revocatedCertificateSchema = new Schema({
    slug: { type: String, required: true, index: true },
    reason: String,
    sn: { type: String, required: true, index: true },
    thumb: { type: String, required: true, index: true },
    active: { type: Boolean, required: true },
    created: { type: Date, default: Date.now },
    createdBy: { type: String, required: true },
    updated: Date,
    updatedBy: String
});

(async () => {
    let connectionString = '';
    if (args.server == 'localhost') {
        connectionString = `mongodb://${args.server}:27017/${args.database}`;
    } else {
        connectionString = `mongodb+srv://${credentials}${args.server}/${args.database}`;
    }
    await mongoose.connect(connectionString).catch((e) => console.error(e));

    const AsymmetricKey = mongoose.model('asymmetric_key', asymmetricKeySchema);
    const Certificate = mongoose.model('certificate', certificateSchema);
    const CertificateAuthority = mongoose.model('certificate_authority', certificateAuthoritySchema);
    const RevocatedCertificate = mongoose.model('revocated_certificate', revocatedCertificateSchema);

    console.info('Creating new collections...');
    await AsymmetricKey.createCollection().catch((e) => console.error(e));
    await Certificate.createCollection().catch((e) => console.error(e));
    await CertificateAuthority.createCollection().catch((e) => console.error(e));
    await RevocatedCertificate.createCollection().catch((e) => console.error(e));

    console.log('Fin');

    await mongoose.disconnect();
    return;
})();
return 0;