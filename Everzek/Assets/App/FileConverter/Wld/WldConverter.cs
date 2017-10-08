﻿
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace OpenEQ.FileConverter.Wld
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Entities;
    using Extensions;
    using UnityEngine;

    public class WldConverter
    {
        public int FlagNormal = 0;
        public int FlagMasked = 1 << 0;
        public int FlagTranslucent = 1 << 1;
        public int FlagTransparent = 1 << 2;

        public WorldHeader Header;
        public Dictionary<int, dynamic> frags;
        public Dictionary<uint, List<object>> byType;
        public Dictionary<string, object> names;
        public bool baked;

        private Dictionary<int, string> _stringTableHash = new Dictionary<int, string>();
        private IDictionary<string, byte[]> s3d;

        public WldConverter()
        {
            byType = new Dictionary<uint, List<object>>();
            frags = new Dictionary<int, dynamic>();
            names = new Dictionary<string, object>();
        }

        private static Dictionary<int, string> GenerateStringTableHash(string input)
        {
            var startPos = 0;
            var output = new Dictionary<int, string>();

            for (var i = 0; i < input.Length; i++)
            {
                if ('\0' != input[i]) continue;

                // Found end of a word.
                output.Add(startPos, input.Substring(startPos, i - startPos));
                startPos = i + 1;
            }

            return output;
        }

        public void Convert(byte[] wldData, IDictionary<string, byte[]> objectFiles)
        {
            s3d = objectFiles;

            using (var input = new BinaryReader(new MemoryStream(wldData)))
            {
                Header = new WorldHeader(input);

                if (0x54503D02 != Header.magic)
                    throw new FormatException("Expected magic (0x54503D02) not found.");

                // Get the string table.
                _stringTableHash = GenerateStringTableHash(input.ReadBytes(Header.stringHashSize).DecodeString());

                // Get all fragments from this file.
                GetFragments(input);

                // Clear the values, but not the keys.
                foreach (var v in byType)
                {
                    v.Value.Clear();
                }

                var nfrags = new Dictionary<int, object>();
                var nnames = new Dictionary<string, object>();

                foreach (var frag in frags.Values.OfType<Tuple<int, string, uint, object>>())
                {
                    nfrags[frag.Item1] = nnames[frag.Item2] = frag.Item4;
                    byType[frag.Item3].Add(frag.Item4);
                }

                frags = nfrags;
                names = nnames;
                baked = true;

                //Console.WriteLine(
                //    $"fragtypes ({byType.Keys.Count}): {byType.Aggregate("", (current, v) => current + $",'{v.Key:X2}'").TrimStart(',')}");
            }
        }

        public void ConvertObjects(Zone zone)
        {
            ConvertMeshFrag(zone);

            // This one needs a fully built list of object names in the zone.
            ConvertObjLocFrag(zone);
        }

        public void ConvertLights(Zone zone)
        {
            // No conversion of lights?
        }

        /*
        /// <summary>
        /// TODO: Finish ConvertCharacters.  Still not fully implemented, but it was 75% or so.
        /// </summary>
        /// <param name="outputFileName"></param>
        /// <param name="zone"></param>
        public void ConvertCharacters(string outputFileName, Zone zone)
        {
            // Delete if it's already there.
            if (File.Exists(outputFileName))
                File.Delete(outputFileName);

            using (var fsOut = File.Create(outputFileName))
            {
                using (var zipArchive = new ZipArchive(fsOut, ZipArchiveMode.Create))
                {
                    foreach (ModelRef modelRef in byType[20])
                    {
                        var charfile = new CharFile(modelRef._name);

                        if (1 != modelRef.skeleton.Length)
                            throw new Exception("ModelRef.Skeleton had more than 1 element.");

                        var skeleton = (SkelPierceTrackSet) modelRef.skeleton[0].Value[0].Value;
                        var roottrackname = skeleton.Tracks[0].pierceTrack[0].Value.Name;

                        var aniTrees = new Dictionary<string, AniTree>();
                        aniTrees.Add("", null);// not sure if I need this.

                        foreach (SkelPierceTrack x in byType[19])
                        {
                            var name = x.Name;

                            if (name != roottrackname && name.EndsWith(roottrackname))
                            {
                                aniTrees.Add(name.Replace(roottrackname, ""), null);
                            }
                        }

                        foreach (var prefix in aniTrees.Keys.ToList())
                        {
                            //aniTrees[prefix] = InvertTree(BuildAniTree(skeleton, prefix, 0));
                        }

                        var meshes = skeleton.Meshes;
                        var voff = 0;

                        foreach (FragRef fr in meshes[0].Value)
                        {
                            var off = 0;
                            var mesh = (FragMesh)fr.Value;

                            foreach (var poly in mesh.Polytex)
                            {
                                var count = poly[0];
                                var index = poly[1];

                                var texnames = ((FragRef[])mesh.Textures[0].Value)[index].Resolve().OfType<string>().ToList();
                                var texFlags = ((TexRef) mesh.Textures[0].Value[index].Value).SaneFlags;
                                var tmpS3DData = texnames.Select(t => s3d[t.ToLower()]).ToList();

                                var material = new Material(texFlags, tmpS3DData);
                                var outpolys = new List<int>();

                                for (var i = off; i < off + count; i++)
                                {
                                    outpolys.Add(voff + (int)mesh.Polys[i].Item2.x);
                                    outpolys.Add(voff + (int)mesh.Polys[i].Item2.y);
                                    outpolys.Add(voff + (int)mesh.Polys[i].Item2.z);
                                }

                                charfile.AddMaterial(material, outpolys);
                                off += count;
                            }

                            voff += mesh.Vertices.Length;
                        }

                        foreach (var aniTree in aniTrees.Values)
                        {
                            foreach (var frame in aniTree.Frames)
                            {
                                BuildBoneMatrices(frame, Matrix4x4.identity);
                            }
                        }
                        var aa = 1;

                        //for modelref in self.byType[0x14]:

                        //    for name, frames in aniTrees.items():
                        //        for i, frame in enumerate(frames):
                        //            mats = { }
                        //            buildBoneMatrices(frame, Matrix44.identity())

                        //            outbuffer = []
                        //            for mesh in meshes:
                        //                inverts = mesh['vertices']
                        //                innorms = mesh['normals']
                        //                texcoords = mesh['texcoords']
                        //                vertices = []
                        //                normals = []
                        //                off = 0
                        //                for count, matid in mesh['bonevertices']:
                        //                    vertices += transform(inverts[off: off + count], mats[matid])
                        //                    normals += transform(innorms[off: off + count], mats[matid])
                        //                    off += count


                        //                temp = flatten(interleave(vertices, normals, texcoords))
                        //                outbuffer += temp
                        //            charfile.addFrame(name, outbuffer)
                        //    charfile.out(zip)
                    }
                }
            }

        //    def convertCharacters(self, zip):

        //        def invertTree(atree):
        //    def getMaxFrames(elem):
        //        return max([len(elem['frames'])] + map(getMaxFrames, elem['children']))
        //    framecount = getMaxFrames(atree)
        //    def sub(elem, i):
        //        return dict(bone = elem['bone'], transform = elem['frames'][i if len(elem['frames']) > 1 else 0], children =[sub(x, i) for x in elem['children']])
        //        frames = []
        //    for i in xrange(framecount):
        //        frames.append(sub(atree, i))
        //    return frames

        //def transform(elems, mat):
        //    return [tuple(mat * Vector3(elem)) for elem in elems]
        }
        */

        private void BuildBoneMatrices(Frame tree, Matrix4x4 parentmat)
        {
            //var trans = tree.
            //    trans = tree['transform']
            //    translate = Matrix44.from_translation(trans['position'])
            //    rotate = Quaternion(trans['rotation']).matrix44
            //    mat = mats[tree['bone']] = rotate * translate * parentmat

            //    for x in tree['children']:
            //        buildBoneMatrices(x, mat)
        }

        private void InvertTree(AniTree atree)
        {
            //def getMaxFrames(elem):
            //    return max([len(elem['frames'])] + map(getMaxFrames, elem['children']))
            //var framecount = getMaxFrames(atree);

            //def sub(elem, i):
            //    return dict(bone = elem['bone'], transform = elem['frames'][i if len(elem['frames']) > 1 else 0], children =[sub(x, i) for x in elem['children']])
            //    frames = []

            //for (var i = 0; i < framecount; i++)
            //{
            //    //    frames.append(sub(atree, i))
            //}
            //return frames
        }

        private AniTree BuildAniTree(SkelPierceTrackSet skeleton, string prefix, int idx)
        {
            var track = skeleton.Tracks[idx];
            var piercetrack = (SkelPierceTrack)track.pierceTrack[0].Value;

            if ("" != prefix && this.names.ContainsKey($"{prefix}{piercetrack.Name}"))
            {
                piercetrack = (SkelPierceTrack)names[$"{prefix}{piercetrack.Name}"];
            }

            var children = new List<AniTree>();
            foreach (var x in track.NextPieces)
            {
                children.Add(BuildAniTree(skeleton, prefix, x));
            }

            return new AniTree(idx, (Frame[]) piercetrack.pierceTrack[0].Value, children);
        }

        public void ConvertZone(Zone zone)
        {
            if (!byType.ContainsKey(54))
                return;

            foreach (FragMesh frag in byType[54])
            {
                var vbuf = new VertexBuffer(frag);

                var off = 0;
                foreach (var polytex in frag.Polytex)
                {
                    var count = polytex[0];
                    var index = polytex[1];

                    if (frag.Textures.Length > 1)
                    {
                        throw new IndexOutOfRangeException(
                            "WldConverter.ConvertObjects -- frag.Textures.Length > 1");
                    }

                    var texnames = ((FragRef[])frag.Textures[0].Value)[index].Resolve().OfType<string>().ToList();
                    var texFlags = ((TexRef)((FragRef[])frag.Textures[0].Value)[index].Value).SaneFlags;

                    var tmpS3DData = texnames.Select(t => s3d[t.ToLower()]).ToList();

                    // The first param was texFlags, but it was always set to 0, so I'm setting it to 0.
                    var material = new Entities.Material(texFlags, tmpS3DData);
                    var mesh = new Entities.Mesh(material, vbuf, frag.Polys.Skip(off).Take(count).ToList());
                    zone.ZoneObj.Meshes.Add(mesh);
                    off += count;
                }
            }
        }

        private void ConvertObjLocFrag(Zone zone)
        {
            if (!byType.ContainsKey(21))
                return;

            foreach (ObjectLocation frag in byType[21])
            {
                var objname = _stringTableHash[-frag.NameOffset].Replace("_ACTORDEF", "");
                zone.AddPlaceable(objname, frag.Position, frag.Rotation, frag.Scale);
            }
        }

        private void ConvertMeshFrag(Zone zone, bool zoneConvert = false)
        {
            if (!byType.ContainsKey(54))
                return;

            foreach (FragMesh frag in byType[54])
            {
                var obj = zone.AddObject(frag._name);
                var vbuf = new VertexBuffer(frag);

                var off = 0;

                foreach (var polytex in frag.Polytex)
                {
                    var count = polytex[0];
                    var index = polytex[1];

                    if (frag.Textures.Length > 1)
                    {
                        throw new IndexOutOfRangeException(
                            "WldConverter.ConvertObjects -- frag.Textures.Length > 1");
                    }

                    var texnames = ((FragRef[])frag.Textures[0].Value)[index].Resolve().OfType<string>().ToList();
                    var texFlags = ((TexRef) ((FragRef[]) frag.Textures[0].Value)[index].Value).SaneFlags;

                    var tmpS3DData = texnames.Select(t => s3d[t.ToLower()]).ToList();

                    var material = new Entities.Material(texFlags, tmpS3DData);
                    var mesh = new Entities.Mesh(material, vbuf, frag.Polys.Skip(off).Take(count).ToList());
                    obj.Meshes.Add(mesh);
                    off += count;
                }
            }
        }

        #region Fragment Handlers

        private void GetFragments(BinaryReader input)
        {
            var foo = new Dictionary<uint, int>();

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < Header.fragmentCount; i++)
            {
                var fragHeader = new struct_wld_basic_frag(input);
                //		fragHeader.nameoff	-15132391	int
                var name =
                    fragHeader.nameoff != 0x1000000 && fragHeader.nameoff != -0x1000000 && _stringTableHash.ContainsKey(-Math.Min(fragHeader.nameoff, 0))
                        ? _stringTableHash[-Math.Min(fragHeader.nameoff, 0)]
                        : "";

                var epos = input.BaseStream.Position + fragHeader.size - 4;
                object frag = null;
                if (!foo.ContainsKey(fragHeader.type))
                {
                    foo.Add(fragHeader.type, 0);
                }
                foo[fragHeader.type]++;
                switch (fragHeader.type)
                {
                    case 3: //0x03
                    {
                        frag = FragTexName(input);
                        break;
                    }
                    case 4: //0x04
                    {
                        frag = FragTexBitInfo(input);
                        break;
                    }
                    case 5: //0x05
                    {
                        frag = FragTexUnk(input);
                        break;
                    }
                    case 16: //0x10
                    {
                        frag = FragSkelTrackSet(input, name);
                        break;
                    }
                    case 17: //0x11
                    {
                        frag = FragSkelTrackSetRef(input);
                        break;
                    }
                    case 18: //0x12
                    {
                        frag = FragSkelPierceTrack(input, name);
                        break;
                    }
                    case 19: //0x13
                    {
                        frag = FragSkelPierceTrackRef(input, name);
                        break;
                    }
                    case 20: //0x14
                    {
                        frag = FragModelRef(input, name);
                        break;
                    }
                    case 21:
                    {
                        frag = FragObjLoc(input, name);
                        break;
                    }
                    case 27:
                    {
                        frag = FragLightSource(input, name);
                        break;
                    }
                    case 28:
                    {
                        frag = FragLightSourceRef(input);
                        break;
                    }
                    case 40:
                    {
                        frag = FragLightInfo(input, name);
                        break;
                    }
                    case 42:
                    {
                        FragAmbient(input);
                        break;
                    }
                    case 45:
                    {
                        frag = FragMeshRef(input);
                        break;
                    }
                    case 48:
                    {
                        frag = FragTexRef(input, name);
                        break;
                    }
                    case 49:
                    {
                        frag = FragTexList(input);
                        break;
                    }
                    case 54:
                    {
                        frag = FragMesh(input, name);
                        break;
                    }
#if DEBUG // Only build in debug mode.  No point in a release build.
                    case 8:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 9:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 22:
                    {
                            // UNKNOWN
                            break;
                    }
                    case 23:
                    {
                        // UNKNOWN
                        Frag23PolyHDef(input, name, fragHeader.size);
                        break;
                    }
                    case 24:
                    {
                        // UNKNOWN
                        Frag24PolyHDef(input, name, fragHeader.size);
                        break;
                    }
                    case 33:
                    {
                        Frag33(input, name);
                            break;
                    }
                    case 34:
                    {
                        Frag34(input, name);
                            break;
                    }
                    case 38:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 41:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 47:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 50:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 51:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 52:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 53:
                    {
                        // UNKNOWN
                        break;
                    }
                    case 55:
                    {
                        // UNKNOWN
                        break;
                    }
#endif
                    //case 0x35: // First fragment
                    //    break;
                    //case 0x21: // BSP Tree
                    //    break;
                    default:
                        Console.WriteLine($"Unsupported fragment type: {fragHeader.type}");
                        break;
                }

                frag = new Tuple<int, string, uint, object>(i, name, fragHeader.type, frag);
                frags[i] = frag;

                if (!string.IsNullOrEmpty(name) || fragHeader.type == 0x05)
                {
                    names[name] = frag;
                }

                // Keep a list of fragments by fragment type.
                if (!byType.ContainsKey(fragHeader.type))
                    byType[fragHeader.type] = new List<object>();

                byType[fragHeader.type].Add(frag);

                input.BaseStream.Position = epos;
            }

            sw.Stop();
            //Console.WriteLine($"{sw.Elapsed}, {sw.ElapsedMilliseconds}, {sw.ElapsedTicks}");
            //foreach (var f in foo)
            //{
            //    Console.WriteLine($"{f.Key} :: {f.Value}");
            //}
        }

        private FragRef[] GetFrag(int reference)
        {
            return GetFrag(new[] {reference});
        }

        private FragRef[] GetFrag(IList<int> references) // r is probably a list of ints
        {
            var refs = new FragRef[references.Count];

            for (var i = 0; i < references.Count; i++)
            {
                if (references[i] > 0)
                {
                    references[i]--;
                    //((Tuple<int, string, uint, object>)frags[references[i]]).Item4
                    if (frags.ContainsKey(references[i]))
                    {
                        refs[i] = new FragRef(references[i],
                            value: ((Tuple<int, string, uint, object>)frags[references[i]]).Item4);
                    }
                    else
                    {
                        refs[i] = new FragRef(id: references[i]);
                    }
                }
                else
                {
                    var name = _stringTableHash[-references[i]];
                    if (names.ContainsKey(name))
                    {
                        refs[i] = new FragRef(name: name, value: names[name]);
                    }
                    else
                    {
                        refs[i] = new FragRef(name: name);
                    }
                }
            }

            return refs;
        }

        private FragRef Frag23PolyHDef(BinaryReader input, string name, uint size)
        {
            var d = input.ReadBytes((int)size);
            return null;
        }

        private FragRef Frag24PolyHDef(BinaryReader input, string name, uint size)
        {
            var d = input.ReadBytes((int)size);
            return null;
        }

        private FragRef Frag33(BinaryReader input, string name)
        {
            var a = 1;
            //FRAGMENT_FUNC(Data21) {
            //    struct_Data21* data;
            //    long count = *((long*)buf);
            //    long i;
            //    BSP_Node* tree = (BSP_Node*)malloc(count * sizeof(BSP_Node));

            //    for (i = 0; i < count; ++i)
            //    {
            //        data = (struct_Data21*)(buf + i * sizeof(struct_Data21));
            //        tree[i].normal[0] = data->normal[0];
            //        tree[i].normal[1] = data->normal[1];
            //        tree[i].normal[2] = data->normal[2];
            //        tree[i].splitdistance = data->splitdistance;
            //        // tree[i].region = (BSP_Region *) wld->frags[data->region]->frag;
            //        // tree[i].left = &tree[data->node[0]];
            //        // tree[i].right = &tree[data->node[1]];
            //    }
            //    return 0;
            //}
            return null;
        }

        private FragRef Frag34(BinaryReader input, string name)
        {
            var a = 1;
            // UNKNOWN
            //FRAGMENT_FUNC(Data22) {
            //    int pos;
            //    struct_Data22* data = (struct_Data22*)buf;

            //    if (!wld->loadBSP)
            //        return -1;

            //    pos = sizeof(struct_Data22) + (12 * data->size1) + (8 * data->size2);

            //    return 0;
            //}
            return null;
        }

        /// <summary>
        /// Handler for fragment ID 0x03
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <returns>A string array containing a filename.</returns>
        /// <remarks>It looks like this is always going to be a single string in an array.  If that holds
        /// true for the newer file format (assuming we use this for that format) then we could probably
        /// make this just return a string.</remarks>
        private static string[] FragTexName(BinaryReader input)
        {
            var size = input.ReadUInt32() + 1;
            var texNames = new string[size];

            for (var i = 0; i < texNames.Length; i++)
            {
                texNames[i] = input.ReadBytes(input.ReadUInt16()).DecodeString().Trim('\0');
            }

            return texNames;
        }

        /// <summary>
        /// Handler for fragment ID 0x04
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <returns>A FragRef array.</returns>
        /// <remarks>This appears to always have 1 child node?</remarks>
        private FragRef[] FragTexBitInfo(BinaryReader input)
        {
            var flags = input.ReadUInt32();
            var size = input.ReadUInt32();

            if (0 != (flags & (1 << 2)))
                input.ReadUInt32();
            if (0 != (flags & (1 << 3)))
                input.ReadUInt32();

            var a = GetFrag(input.ReadInt32(size));
            return a;
        }

        /// <summary>
        /// 0x05
        /// </summary>
        /// <param name="input"></param>
        /// <returns>An array of FragRefs</returns>
        /// <remarks>This appears to be a FragRef with one or more references to a FragRef
        /// that has the underlying file name.</remarks>
        private FragRef[] FragTexUnk(BinaryReader input)
        {
            var a = GetFrag(input.ReadInt32());
            return a;
        }

        /// <summary>
        /// 0x10
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        private SkelPierceTrackSet FragSkelTrackSet(BinaryReader input, string name)
        {
            var flags = input.ReadUInt32();
            var trackcount = input.ReadUInt32();

            // We weren't using the output.  Not sure why this is here?
            GetFrag(input.ReadInt32());

            if (0 != (flags & 1))
                input.ReadUInt32(3);

            if (0 != (flags & 2))
                input.ReadSingle();

            var trackSet = new SkelPierceTrackSet(trackcount, name);

            var allMeshes = new List<int>();
            for (var i = 0; i < trackcount; i++)
            {
                var track = new SkelPierceTrack
                {
                    Name = _stringTableHash[-input.ReadInt32()],
                    flags = input.ReadUInt32(),
                    pierceTrack = GetFrag(input.ReadInt32())
                };

                allMeshes.Add(input.ReadInt32());
                track.NextPieces = input.ReadInt32(input.ReadUInt32());
                trackSet.Tracks[i] = track;
            }

            var meshes = new List<FragRef>();

            if (0 != (flags & 0x200))
                meshes.AddRange(GetFrag(input.ReadInt32(input.ReadUInt32())));
            else
            {
                for (var i = 0; i < allMeshes.Count(); i++)
                {
                    if (0 != allMeshes[i])
                        meshes.AddRange(GetFrag(allMeshes[i]));
                }
            }

            trackSet.Meshes = meshes;
            return trackSet;
        }

        /// <summary>
        /// 0x11
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private FragRef[] FragSkelTrackSetRef(BinaryReader input)
        {
            return GetFrag(input.ReadInt32());
        }

        /// <summary>
        /// 0x12
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private Frame[] FragSkelPierceTrack(BinaryReader input, string name)
        {
            // Skipping flags?
            input.ReadUInt32();

            //var large = (flags & 8) > 0;

            var framecount = input.ReadUInt32();

            var frames = new Frame[framecount];

            for (var i = 0; i < framecount; i++)
            {
                var rotw = (float) input.ReadInt16();
                var rotx = (float) input.ReadInt16();
                var roty = (float) input.ReadInt16();
                var rotz = (float) input.ReadInt16();

                var shiftx = (float) input.ReadInt16();
                var shifty = (float) input.ReadInt16();
                var shiftz = (float) input.ReadInt16();
                var shiftden = (float) input.ReadInt16();

                if (0 != rotw)
                {
                    rotx /= 16384F;
                    roty /= 16384F;
                    rotz /= 16384F;
                    rotw /= 16384F;
                }
                else
                {
                    rotx = roty = rotz = 0;
                    rotw = 1;
                }

                if (0 != shiftden)
                {
                    shiftx /= shiftden;
                    shifty /= shiftden;
                    shiftz /= shiftden;
                }
                else
                {
                    shiftx = shifty = shiftz = 0;
                }

                frames[i] = new Frame(name, shiftx, shifty, shiftz, -rotx, -roty, -rotz, rotw);
            }

            return frames;
        }

        /// <summary>
        /// 0x13
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private SkelPierceTrack FragSkelPierceTrackRef(BinaryReader input, string name)
        {
            var skelpiecetrack = GetFrag(input.ReadInt32());
            var flags = input.ReadUInt32();

            if (0 != (flags & 1))
                input.ReadUInt32();

            return new SkelPierceTrack(name, skelpiecetrack);
        }

        /// <summary>
        /// Handler for 0x14
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        private ModelRef FragModelRef(BinaryReader input, string name)
        {
            var flags = input.ReadUInt32();

            // Skip this.
            input.ReadUInt32();

            var size1 = input.ReadUInt32();
            var size2 = input.ReadUInt32();

            // Skip this.
            input.ReadUInt32();

            if (0 != (flags & 1))
                input.ReadUInt32();

            if (0 != (flags & 2))
                input.ReadUInt32();

            for (var i = 0; i < size1; i++)
            {
                var e1size = input.ReadUInt32();
                var eldata = new float[e1size];

                for (var j = 0; j < e1size; j++)
                {
                    eldata[input.ReadUInt32()] = input.ReadSingle();
                }

                var aaa = 1;
            }

            var frags3 = input.ReadInt32(size2);

            // A string, but it seems to be blank?  Skipping it anyway.
            input.ReadBytes(input.ReadInt32()).DecodeString();
            var aa = new ModelRef(name, GetFrag(frags3));
            return aa;
        }

        /// <summary>
        /// Handler for 0x15
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        private static ObjectLocation FragObjLoc(BinaryReader input, string name)
        {
            var nameOffset = input.ReadInt32();

            // flags, we just want to skip them.
            input.ReadUInt32();

            // Some unknown value that we're skipping.
            input.ReadUInt32();
            var pos = input.ReadSingle(3);
            var rot = input.ReadSingle(3);

            var tmpRot = new float[rot.Length];
            tmpRot[0] = (rot[2]/512F)*360F* (float)(Math.PI/180F);
            tmpRot[1] = (rot[1]/512F)*360F* (float)(Math.PI/180F);
            tmpRot[2] = (rot[0]/512F)*360F* (float)(Math.PI/180F);
            rot = tmpRot;

            var scale = input.ReadSingle(3);
            scale = scale[2] > 0.0001 ? new[] {scale[2], scale[2], scale[2]} : new[] {1F, 1F, 1F};

            // Some unknown value that we're skipping.
            input.ReadUInt32();

            // Params? that we're skipping.
            input.ReadUInt32();

            return new ObjectLocation(name, pos, rot, scale, nameOffset);
        }

        /// <summary>
        /// Handler for 0x18
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        private static LightSource FragLightSource(BinaryReader input, string name)
        {
            var flags = input.ReadUInt32();

            // Params?  That we're skipping.
            input.ReadUInt32();

            var attenuation = 200.0F;
            float[] color;

            if (0 != (flags & (1 << 4)))
            {
                if (0 != (flags & (1 << 3)))
                {
                    attenuation = input.ReadUInt32();
                }

                // Skipping.
                input.ReadSingle();

                color = input.ReadSingle(3);
            }
            else
            {
                var params3A = input.ReadSingle();
                color = new[] {params3A, params3A, params3A};
            }

            return new LightSource {_name = name, Attenuation = attenuation, Color = color};
        }

        /// <summary>
        /// 0x1C
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private FragRef[] FragLightSourceRef(BinaryReader input)
        {
            return GetFrag(input.ReadInt32());
        }

        /// <summary>
        /// 0x28
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        private LightInfo FragLightInfo(BinaryReader input, string name)
        {
            var lref = GetFrag(input.ReadInt32());
            var flags = input.ReadUInt32();
            var pos = input.ReadSingle(3);
            var radius = input.ReadSingle();

            return new LightInfo(name, lref, flags, pos, radius);
        }

        /// <summary>
        /// 0x2A
        /// </summary>
        /// <param name="input"></param>
        private void FragAmbient(BinaryReader input)
        {
            GetFrag(input.ReadInt32());
            input.ReadUInt32();
            input.ReadUInt32(input.ReadUInt32());
        }

        /// <summary>
        /// 0x2D
        /// </summary>
        /// <param name="input"></param>
        private FragRef[] FragMeshRef(BinaryReader input)
        {
            return GetFrag(input.ReadInt32());
        }

        /// <summary>
        /// 0x30
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private TexRef FragTexRef(BinaryReader input, string name)
        {
            var pairflags = input.ReadUInt32();
            var flags = input.ReadUInt32();
            input.BaseStream.Position += 12;

            if (2 == (pairflags & 2))
                input.BaseStream.Position += 8;

            var saneflags = 0;

            if (flags == 0)
                saneflags = FlagTransparent;

            if ((flags & (2 | 8 | 16)) != 0)
                saneflags |= FlagMasked;

            if ((flags & (4 | 8)) != 0)
                saneflags |= FlagTranslucent;

            return new TexRef(name, saneflags, GetFrag(input.ReadInt32()));
        }

        /// <summary>
        /// 0x31
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private FragRef[] FragTexList(BinaryReader input)
        {
            input.BaseStream.Position += 4;
            return GetFrag(input.ReadInt32(input.ReadUInt32()));
        }

        /// <summary>
        /// 0x36
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        private FragMesh FragMesh(BinaryReader input, string name)
        {
            // Flags?  That we're skipping.
            input.ReadUInt32();

            var tlistref = input.ReadInt32();

            // Skip whatever this is.
            input.ReadUInt32();

            return new FragMesh(input, Header.IsOldVersion, GetFrag(tlistref), name);
        }

        #endregion
    }
}