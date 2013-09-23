// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;

    // <summary>
    // Class holding utility functions for metadata
    // </summary>
    internal static class Util
    {
        // <summary>
        // Throws an appropriate exception if the given item is a readonly, used when an attempt is made to change
        // a property
        // </summary>
        // <param name="item"> The item whose readonly is being tested </param>
        internal static void ThrowIfReadOnly(MetadataItem item)
        {
            DebugCheck.NotNull(item);
            if (item.IsReadOnly)
            {
                throw new InvalidOperationException(Strings.OperationOnReadOnlyItem);
            }
        }

        // <summary>
        // Check to make sure the given item do have identity
        // </summary>
        // <param name="item"> The item to check for valid identity </param>
        // <param name="argumentName"> The name of the argument </param>
        [Conditional("DEBUG")]
        internal static void AssertItemHasIdentity(MetadataItem item, string argumentName)
        {
            DebugCheck.NotEmpty(item.Identity);
            Check.NotNull(item, argumentName);
        }

        // <summary>
        // Retrieves a mapping to CLR type for the given EDM type. Assumes the MetadataWorkspace has no
        // </summary>
        internal static ObjectTypeMapping GetObjectMapping(EdmType type, MetadataWorkspace workspace)
        {
            // Check if the workspace has cspace item collection registered with it. If not, then its a case
            // of public materializer trying to create objects from PODR or EntityDataReader with no context.
            ItemCollection collection;
            if (workspace.TryGetItemCollection(DataSpace.CSpace, out collection))
            {
                return (ObjectTypeMapping)workspace.GetMap(type, DataSpace.OCSpace);
            }
            else
            {
                EdmType ospaceType;
                EdmType cspaceType;
                // If its a case of EntityDataReader with no context, the typeUsage which is passed in must contain
                // a cspace type. We need to look up an OSpace type in the ospace item collection and then create
                // ocMapping
                if (type.DataSpace
                    == DataSpace.CSpace)
                {
                    // if its a primitive type, then the names will be different for CSpace type and OSpace type
                    if (Helper.IsPrimitiveType(type))
                    {
                        ospaceType = workspace.GetMappedPrimitiveType(((PrimitiveType)type).PrimitiveTypeKind, DataSpace.OSpace);
                    }
                    else
                    {
                        // Metadata will throw if there is no item with this identity present.
                        // Is this exception fine or does object materializer code wants to wrap and throw a new exception
                        ospaceType = workspace.GetItem<EdmType>(type.FullName, DataSpace.OSpace);
                    }
                    cspaceType = type;
                }
                else
                {
                    // In case of PODR, there is no cspace at all. We must create a fake ocmapping, with ospace types
                    // on both the ends
                    ospaceType = type;
                    cspaceType = type;
                }

                // This condition must be hit only when someone is trying to materialize a legacy data reader and we
                // don't have the CSpace metadata.
                if (!Helper.IsPrimitiveType(ospaceType)
                    && !Helper.IsEntityType(ospaceType)
                    && !Helper.IsComplexType(ospaceType))
                {
                    throw new NotSupportedException(Strings.Materializer_UnsupportedType);
                }

                ObjectTypeMapping typeMapping;

                if (Helper.IsPrimitiveType(ospaceType))
                {
                    typeMapping = new ObjectTypeMapping(ospaceType, cspaceType);
                }
                else
                {
                    typeMapping = DefaultObjectMappingItemCollection.LoadObjectMapping(cspaceType, ospaceType, null);
                }

                return typeMapping;
            }
        }
    }
}
