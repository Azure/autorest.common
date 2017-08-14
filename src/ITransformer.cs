// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// 

namespace AutoRest.Core {
    using Model;

    public interface ITransformer<out TResultCodeModel> where TResultCodeModel : CodeModel {
        TResultCodeModel TransformCodeModel(CodeModel codeModel);
    }
}