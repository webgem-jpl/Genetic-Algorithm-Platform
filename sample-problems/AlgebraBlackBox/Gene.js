var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
(function (dependencies, factory) {
    if (typeof module === 'object' && typeof module.exports === 'object') {
        var v = factory(require, exports); if (v !== undefined) module.exports = v;
    }
    else if (typeof define === 'function' && define.amd) {
        define(dependencies, factory);
    }
})(["require", "exports", "../../source/GeneBase", "typescript-dotnet-umd/System.Linq/Linq", "typescript-dotnet-umd/System/Text/Utility"], function (require, exports) {
    "use strict";
    var GeneBase_1 = require("../../source/GeneBase");
    var Linq_1 = require("typescript-dotnet-umd/System.Linq/Linq");
    var Utility_1 = require("typescript-dotnet-umd/System/Text/Utility");
    var EMPTY = "";
    var VARIABLE_NAMES = Object.freeze(Linq_1.Enumerable("abcdefghijklmnopqrstuvwxyz").toArray());
    function toAlphaParameters(hash) {
        return Utility_1.supplant(hash, VARIABLE_NAMES);
    }
    exports.toAlphaParameters = toAlphaParameters;
    var AlgebraGene = (function (_super) {
        __extends(AlgebraGene, _super);
        function AlgebraGene(_multiple) {
            if (_multiple === void 0) { _multiple = 1; }
            var _this = _super.call(this) || this;
            _this._multiple = _multiple;
            return _this;
        }
        AlgebraGene.prototype.serialize = function () {
            return this.toString();
        };
        Object.defineProperty(AlgebraGene.prototype, "multiple", {
            get: function () {
                return this._multiple;
            },
            set: function (value) {
                if (this._multiple != value) {
                    this._multiple = value;
                    this._onModified();
                }
            },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(AlgebraGene.prototype, "multiplePrefix", {
            get: function () {
                var m = this._multiple;
                if (m != 1)
                    return m == -1 ? "-" : (m + EMPTY);
                return EMPTY;
            },
            enumerable: true,
            configurable: true
        });
        AlgebraGene.prototype.toStringInternal = function () {
            return this.multiplePrefix
                + this.toStringContents();
        };
        AlgebraGene.prototype.toAlphaParameters = function () {
            return toAlphaParameters(this.toString());
        };
        AlgebraGene.prototype.calculate = function (values) {
            return this._multiple
                * this.calculateWithoutMultiple(values);
        };
        AlgebraGene.prototype.toEntityWithoutMultiple = function () {
            return this.toStringContents();
        };
        AlgebraGene.prototype.toEntity = function () {
            if (this.multiple == 0)
                return "0";
            var prefix = this.multiplePrefix;
            var suffix = this.toEntityWithoutMultiple();
            if (prefix && suffix)
                return (prefix == "-" ? prefix : (prefix + "*")) + suffix;
            if (suffix)
                return suffix;
            if (prefix)
                return prefix;
            throw "No entity.";
        };
        return AlgebraGene;
    }(GeneBase_1.default));
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.default = AlgebraGene;
});
//# sourceMappingURL=Gene.js.map