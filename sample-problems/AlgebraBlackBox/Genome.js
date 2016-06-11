"use strict";
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var Genome_1 = require("../../source/Genome");
var InvalidOperationException_1 = require("typescript-dotnet-umd/System/Exceptions/InvalidOperationException");
var AlgebraGenome = (function (_super) {
    __extends(AlgebraGenome, _super);
    function AlgebraGenome(root) {
        _super.call(this, root);
    }
    AlgebraGenome.prototype.clone = function () {
        return new AlgebraGenome(this.root.clone());
    };
    AlgebraGenome.prototype.serialize = function () {
        var root = this.root;
        if (!root)
            throw new InvalidOperationException_1.default("Cannot calculate a gene with no root.");
        return root.serialize();
    };
    AlgebraGenome.prototype.serializeReduced = function () {
        var root = this.root;
        if (!root)
            throw new InvalidOperationException_1.default("Cannot calculate a gene with no root.");
        return root.asReduced().serialize();
    };
    Object.defineProperty(AlgebraGenome.prototype, "hashReduced", {
        get: function () {
            return this._hashReduced || (this._hashReduced = this.serializeReduced());
        },
        enumerable: true,
        configurable: true
    });
    AlgebraGenome.prototype.resetHash = function () {
        _super.prototype.resetHash.call(this);
        this._hashReduced = null;
    };
    AlgebraGenome.prototype.calculate = function (values) {
        var root = this.root;
        if (!root)
            throw new InvalidOperationException_1.default("Cannot calculate a gene with no root.");
        return root.calculate(values);
    };
    return AlgebraGenome;
}(Genome_1.default));
Object.defineProperty(exports, "__esModule", { value: true });
exports.default = AlgebraGenome;
//# sourceMappingURL=Genome.js.map