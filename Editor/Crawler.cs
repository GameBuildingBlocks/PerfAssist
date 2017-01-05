using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.MemoryProfiler;

namespace MemoryProfilerWindow
{
    internal class Crawler
    {
        private Dictionary<UInt64, TypeDescription> _typeInfoToTypeDescription;

        private Dictionary<int, UInt64> _pointer2Backups = new Dictionary<int, ulong>();
        private VirtualMachineInformation _virtualMachineInformation;
        private TypeDescription[] _typeDescriptions;

        public PackedCrawlerData Crawl(PackedMemorySnapshot input)
        {
            _typeInfoToTypeDescription = input.typeDescriptions.ToDictionary(td => td.typeInfoAddress, td => td);
            _virtualMachineInformation = input.virtualMachineInformation;
            _typeDescriptions = input.typeDescriptions;

            MemUtil.LoadSnapshotProgress(0.1f, "adding connections");
            var result = new PackedCrawlerData(input);
            var managedObjects = new List<PackedManagedObject>(result.startIndices.OfFirstManagedObject * 3);

            var connections = new List<Connection>(managedObjects.Count * 3);
            //we will be adding a lot of connections, but the input format also already had connections. (nativeobject->nativeobject and nativeobject->gchandle). we'll add ours to the ones already there.
            connections.AddRange(input.connections);

            MemUtil.LoadSnapshotProgress(0.2f, "CrawlPointer");
            for (int i = 0; i != input.gcHandles.Length; i++)
            {
                CrawlPointer(input, result.startIndices, input.gcHandles[i].target, result.startIndices.OfFirstGCHandle + i, connections, managedObjects);
                
                if (i % 5 == 0)
                    MemUtil.LoadSnapshotProgress(0.2f + 0.2f * ((float)i / (float)input.gcHandles.Length), 
                        string.Format("CrawlPointer({0}/{1})", i, input.gcHandles.Length));
            }

            MemUtil.LoadSnapshotProgress(0.4f, "CrawlRawObjectData");
            for (int i = 0; i < result.typesWithStaticFields.Length; i++)
            {
                var typeDescription = result.typesWithStaticFields[i];
                CrawlRawObjectData(input, result.startIndices, new BytesAndOffset {bytes = typeDescription.staticFieldBytes, offset = 0, pointerSize = _virtualMachineInformation.pointerSize}, typeDescription, true, result.startIndices.OfFirstStaticFields + i, connections, managedObjects);

                if (i % 5 == 0)
                    MemUtil.LoadSnapshotProgress(0.4f + 0.2f * ((float)i / (float)result.typesWithStaticFields.Length),
                        string.Format("CrawlRawObjectData({0}/{1})", i, result.typesWithStaticFields.Length));
            }

            MemUtil.LoadSnapshotProgress(0.6f, "composing result");
            result.managedObjects = managedObjects.ToArray();
            connections.AddRange(AddManagedToNativeConnectionsAndRestoreObjectHeaders(input, result.startIndices, result));
            result.connections = connections.ToArray();

            return result;
        }

        private IEnumerable<Connection> AddManagedToNativeConnectionsAndRestoreObjectHeaders(PackedMemorySnapshot packedMemorySnapshot, StartIndices startIndices, PackedCrawlerData packedCrawlerData)
        {
            if (packedMemorySnapshot.typeDescriptions.Length == 0)
                yield break;

            var unityEngineObjectTypeDescription = packedMemorySnapshot.typeDescriptions.First(td => td.name == "UnityEngine.Object");

            bool unityEngineObjectHasInstanceIDField = unityEngineObjectTypeDescription.fields.Any(f => f.name == "m_InstanceID");
            int instanceIDOffset = -1;

            if (unityEngineObjectHasInstanceIDField)
                instanceIDOffset = unityEngineObjectTypeDescription.fields.Single(f => f.name == "m_InstanceID").offset;

#if UNITY_5_4_OR_NEWER
            var cachedPtrOffset = unityEngineObjectTypeDescription.fields.Single(f => f.name == "m_CachedPtr").offset;
#endif

            for (int i = 0; i != packedCrawlerData.managedObjects.Length; i++)
            {
                var managedObjectIndex = i + startIndices.OfFirstManagedObject;
                var address = packedCrawlerData.managedObjects[i].address;

                var typeInfoAddress = RestoreObjectHeader(packedMemorySnapshot.managedHeapSections, address, managedObjectIndex);

                if (!DerivesFrom(packedMemorySnapshot.typeDescriptions, _typeInfoToTypeDescription[typeInfoAddress].typeIndex, unityEngineObjectTypeDescription.typeIndex))
                    continue;

                int indexOfNativeObject = -1;
                if (unityEngineObjectHasInstanceIDField)
                {
                    var instanceID = packedMemorySnapshot.managedHeapSections.Find(address + (UInt64)instanceIDOffset, packedMemorySnapshot.virtualMachineInformation).ReadInt32();
                    indexOfNativeObject = Array.FindIndex(packedMemorySnapshot.nativeObjects, no => no.instanceId == instanceID);
                }
#if UNITY_5_4_OR_NEWER // Since Unity 5.4, UnityEngine.Object no longer stores instance id inside when running in the player. Use cached ptr instead to find the index of native object
                else
                {
                    // If you get a compilation error on the following 2 lines, update to Unity 5.4b14.
                    var cachedPtr = packedMemorySnapshot.managedHeapSections.Find(address + (UInt64)cachedPtrOffset, packedMemorySnapshot.virtualMachineInformation).ReadPointer();
                    indexOfNativeObject = Array.FindIndex(packedMemorySnapshot.nativeObjects, no => (ulong)no.nativeObjectAddress == cachedPtr);
                }
#endif

                if (indexOfNativeObject != -1)
                    yield return new Connection { @from = managedObjectIndex, to = indexOfNativeObject + startIndices.OfFirstNativeObject };
            }
        }

        private bool DerivesFrom(TypeDescription[] typeDescriptions, int typeIndex, int potentialBase)
        {
            if (typeIndex == potentialBase)
                return true;
            var baseIndex = typeDescriptions[typeIndex].baseOrElementTypeIndex;

            if (baseIndex == -1)
                return false;

            return DerivesFrom(typeDescriptions, baseIndex, potentialBase);
        }

        private ulong RestoreObjectHeader(MemorySection[] heaps, ulong address, int managedObjectIndex)
        {
            var bo = heaps.Find(address, _virtualMachineInformation);
            var mask = this._virtualMachineInformation.pointerSize == 8 ? System.UInt64.MaxValue - 1 : System.UInt32.MaxValue - 1;
            var pointer = bo.ReadPointer();
            var typeInfoAddress = pointer & mask;
            bo.WritePointer(typeInfoAddress);

            UInt64 restoreValue = 0;
            _pointer2Backups.TryGetValue(managedObjectIndex, out restoreValue);
            bo.NextPointer().WritePointer(restoreValue);
            return typeInfoAddress;
        }

        private void CrawlRawObjectData(PackedMemorySnapshot packedMemorySnapshot, StartIndices startIndices, BytesAndOffset bytesAndOffset, TypeDescription typeDescription, bool useStaticFields, int indexOfFrom, List<Connection>  out_connections, List<PackedManagedObject> out_managedObjects)
        {
            foreach (var field in TypeTools.AllFieldsOf(typeDescription, _typeDescriptions, useStaticFields ? TypeTools.FieldFindOptions.OnlyStatic : TypeTools.FieldFindOptions.OnlyInstance))
            {
                if (field.typeIndex == typeDescription.typeIndex && typeDescription.isValueType)
                {
                    //this happens in System.Single, which is a weird type that has a field of its own type.
                    continue;
                }

                if (field.offset == -1)
                {
                    //this is how we encode TLS fields. todo: actually treat TLS fields as roots
                    continue;
                }

                var fieldType = packedMemorySnapshot.typeDescriptions[field.typeIndex];

                var fieldLocation = bytesAndOffset.Add(field.offset - (useStaticFields ? 0 : _virtualMachineInformation.objectHeaderSize));

                if (fieldType.isValueType)
                {
                    CrawlRawObjectData(packedMemorySnapshot, startIndices, fieldLocation, fieldType, false, indexOfFrom, out_connections, out_managedObjects);
                    continue;
                }

                //temporary workaround for a bug in 5.3b4 and earlier where we would get literals returned as fields with offset 0. soon we'll be able to remove this code.
                bool gotException = false;
                try
                {
                    fieldLocation.ReadPointer();
                }
                catch (ArgumentException)
                {
                    UnityEngine.Debug.LogWarningFormat("Skipping field {0} on type {1}", field.name, typeDescription.name);
                    UnityEngine.Debug.LogWarningFormat("FieldType.name: {0}", fieldType.name);
                    gotException = true;
                }

                if (!gotException)
                {
                    CrawlPointer(packedMemorySnapshot, startIndices, fieldLocation.ReadPointer(), indexOfFrom, out_connections, out_managedObjects);
                }
            }
        }

        private void CrawlPointer(PackedMemorySnapshot packedMemorySnapshot, StartIndices startIndices, ulong pointer, int indexOfFrom, List<Connection> out_connections, List<PackedManagedObject> out_managedObjects)
        {
            var bo = packedMemorySnapshot.managedHeapSections.Find(pointer, _virtualMachineInformation);
            if (!bo.IsValid)
                return;

            UInt64 typeInfoAddress;
            int indexOfObject;
            bool wasAlreadyCrawled;
            try
            {
                ParseObjectHeader(startIndices, packedMemorySnapshot.managedHeapSections, pointer, out typeInfoAddress, out indexOfObject, out wasAlreadyCrawled, out_managedObjects);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarningFormat("Exception parsing object header. Skipping. {0}", e);
                return;
            }

            out_connections.Add(new Connection() {from = indexOfFrom, to = indexOfObject});

            if (wasAlreadyCrawled)
                return;

            var typeDescription = _typeInfoToTypeDescription[typeInfoAddress];

            if (!typeDescription.isArray)
            {
                CrawlRawObjectData(packedMemorySnapshot, startIndices, bo.Add(_virtualMachineInformation.objectHeaderSize), typeDescription, false, indexOfObject, out_connections, out_managedObjects);
                return;
            }

            var arrayLength = ArrayTools.ReadArrayLength(packedMemorySnapshot.managedHeapSections, pointer, typeDescription, _virtualMachineInformation);
            var elementType = packedMemorySnapshot.typeDescriptions[typeDescription.baseOrElementTypeIndex];
            var cursor = bo.Add(_virtualMachineInformation.arrayHeaderSize);
            for (int i = 0; i != arrayLength; i++)
            {
                if (elementType.isValueType)
                {
                    CrawlRawObjectData(packedMemorySnapshot, startIndices, cursor, elementType, false, indexOfObject, out_connections, out_managedObjects);
                    cursor = cursor.Add(elementType.size);
                }
                else
                {
                    CrawlPointer(packedMemorySnapshot, startIndices, cursor.ReadPointer(), indexOfObject, out_connections, out_managedObjects);
                    cursor = cursor.NextPointer();
                }
            }
        }

        int SizeOfObjectInBytes(TypeDescription typeDescription, BytesAndOffset bo, MemorySection[] heap, ulong address)
        {
            if (typeDescription.isArray)
                return ArrayTools.ReadArrayObjectSizeInBytes(heap, address, typeDescription, _typeDescriptions, _virtualMachineInformation);

            if (typeDescription.name == "System.String")
                return StringTools.ReadStringObjectSizeInBytes(bo, _virtualMachineInformation);

            //array and string are the only types that are special, all other types just have one size, which is stored in the typedescription
            return typeDescription.size;
        }

        private void ParseObjectHeader(StartIndices startIndices, MemorySection[] heap, ulong originalHeapAddress, out ulong typeInfoAddress, out int indexOfObject, out bool wasAlreadyCrawled, List<PackedManagedObject> outManagedObjects)
        {
            var bo = heap.Find(originalHeapAddress, _virtualMachineInformation);

            var pointer1 = bo.ReadPointer();
            var pointer2 = bo.NextPointer();

            if (HasMarkBit(pointer1) == 0)
            {
                wasAlreadyCrawled = false;
                indexOfObject = outManagedObjects.Count + startIndices.OfFirstManagedObject;
                typeInfoAddress = pointer1;
                var typeDescription = _typeInfoToTypeDescription[pointer1];


                var size = SizeOfObjectInBytes(typeDescription, bo, heap, originalHeapAddress);

                outManagedObjects.Add(new PackedManagedObject() { address = originalHeapAddress, size = size, typeIndex = typeDescription.typeIndex });

                //okay, we gathered all information, now lets set the mark bit, and store the index for this object in the 2nd pointer of the header, which is rarely used.
                bo.WritePointer(pointer1 | 1);

                //test writepointer implementation
                ulong magic = bo.pointerSize == 8 ? 0x12345678deadbeefUL : 0xdeadbeef;

                pointer2.WritePointer(magic);
                var check = pointer2.ReadPointer();
                if (check != magic)
                    throw new Exception("writepointer broken");

                pointer2.WritePointer((ulong)indexOfObject);
                return;
            }

            //give typeinfo address back without the markbit
            typeInfoAddress = ClearMarkBit(pointer1);
            wasAlreadyCrawled = true;
            //read the index for this object that we stored in the 2ndpointer field of the header
            indexOfObject = (int)pointer2.ReadPointer();
        }

        private static ulong HasMarkBit(ulong pointer1)
        {
            return pointer1 & 1;
        }

        private static ulong ClearMarkBit(ulong pointer1)
        {
            return pointer1 & ~(1UL);
        }
    }
}
