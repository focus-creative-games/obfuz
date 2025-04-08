using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.W32Resources;
using ImageCor20Header = dnlib.DotNet.Writer.ImageCor20Header;

namespace dnlib.Protection {

	/// <summary>
	/// 
	/// </summary>
	public sealed class EncryptedModuleWriterOptions : ModuleWriterOptionsBase {

		public RandomEncryption encryptor;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public EncryptedModuleWriterOptions(ModuleDef module) : base(module) { }
	}

	/// <summary>
	/// 
	/// </summary>
	public sealed class EncryptedModuleWriter : ModuleWriterBase {


		readonly ModuleDef module;
		EncryptedModuleWriterOptions options;
		List<PESection> sections;
		PESection textSection;


		EncryptionHeader header;

		/// <inheritdoc/>
		public override ModuleDef Module => module;

		/// <inheritdoc/>
		public override ModuleWriterOptionsBase TheOptions => Options;

		/// <summary>
		/// Gets/sets the writer options. This is never <c>null</c>
		/// </summary>
		public EncryptedModuleWriterOptions Options {
			get => options ??= new EncryptedModuleWriterOptions(module);
			set => options = value;
		}

		/// <summary>
		/// Gets all <see cref="PESection"/>s. The reloc section must be the last section, so use <see cref="AddSection(PESection)"/> if you need to append a section
		/// </summary>
		public override List<PESection> Sections => sections;

		/// <summary>
		/// Adds <paramref name="section"/> to the sections list, but before the reloc section which must be last
		/// </summary>
		/// <param name="section">New section to add to the list</param>
		public override void AddSection(PESection section) {
			//if (sections.Count > 0 && sections[sections.Count - 1] == relocSection)
			//	sections.Insert(sections.Count - 1, section);
			//else
				sections.Add(section);
		}

		/// <summary>
		/// Gets the <c>.text</c> section
		/// </summary>
		public override PESection TextSection => textSection;

		/// <summary>
		/// Gets the <c>.rsrc</c> section or null if none
		/// </summary>
		public override PESection RsrcSection => null;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public EncryptedModuleWriter(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		/// <param name="options">Options or <c>null</c></param>
		public EncryptedModuleWriter(ModuleDef module, EncryptedModuleWriterOptions options) {
			this.module = module;
			this.options = options;
		}

		/// <inheritdoc/>
		protected override long WriteImpl() {
			try {
				EncryptionContext.Encryption = options.encryptor;
				Initialize();
				metadata.CreateTables();
				return WriteFile();
			}
			finally {
				EncryptionContext.Encryption = null;
			}
		}

		void Initialize() {
			CreateSections();
			OnWriterEvent(ModuleWriterEvent.PESectionsCreated);

			CreateChunks();
			OnWriterEvent(ModuleWriterEvent.ChunksCreated);

			AddChunksToSections();
			OnWriterEvent(ModuleWriterEvent.ChunksAddedToSections);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources() {
			return null;
		}

		void CreateSections() {
			sections = new List<PESection>();
			sections.Add(textSection = new PESection(".text", 0x60000020));
		}

		void CreateChunks() {
			IEncryption enc = EncryptionContext.Encryption;
			header = new EncryptionHeader() {
				formatVersion = 1,
				encryptionInfo = new EncryptionInfo() {
					algorithm = options.encryptor.Algorithm,
					version = options.encryptor.AlgoVersion,
					key = options.encryptor.EncParam,
					headerEncOpCodes = enc.FileHeaderEnc.GetDecryptionOpCode(),
					stringEncOpCodes = enc.StringEnc.GetDecryptionOpCode(),
					blobEncOpCodes = enc.BlobEnc.GetDecryptionOpCode(),
					userStringEncOpCodes = enc.UserStringEnc.GetDecryptionOpCode(),
					lazyUserStringEncOpCodes = enc.LazyUserStringEnc.GetDecryptionOpCode(),
					tableEncOpCodes = enc.TableEnc.GetDecryptionOpCode(),
					lazyTableEncOpCodes = enc.LazyTableEnc.GetDecryptionOpCode(),
					methodBoyEncOpCodes = enc.MethodBodyEnc.GetDecryptionOpCode(),
				},
			};
			CreateMetadataChunks(module);
		}

		void AddChunksToSections() {
			textSection.Add(constants, DEFAULT_CONSTANTS_ALIGNMENT);
			textSection.Add(methodBodies, DEFAULT_METHODBODIES_ALIGNMENT);
			textSection.Add(metadata, DEFAULT_METADATA_ALIGNMENT);
		}

		long WriteFile() {
			//managedExportsWriter.AddExportedMethods(metadata.ExportedMethods, GetTimeDateStamp());
			//if (managedExportsWriter.HasExports)
			//	needStartupStub = true;

			//OnWriterEvent(ModuleWriterEvent.BeginWritePdb);
			//WritePdbFile();
			//OnWriterEvent(ModuleWriterEvent.EndWritePdb);

			metadata.OnBeforeSetOffset();
			OnWriterEvent(ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);
			var chunks = new List<IChunk>();
			chunks.Add(header);
			//chunks.Add(peHeaders);
			//if (!managedExportsWriter.HasExports)
			//	sections.Remove(sdataSection);
			//if (!(relocDirectory.NeedsRelocSection || managedExportsWriter.HasExports || needStartupStub))
			//	sections.Remove(relocSection);

			//importAddressTable.Enable = needStartupStub;
			//importDirectory.Enable = needStartupStub;
			//startupStub.Enable = needStartupStub;

			foreach (var section in sections)
				chunks.Add(section);
			header.sections = sections;
			//peHeaders.PESections = sections;
			//int relocIndex = sections.IndexOf(relocSection);
			//if (relocIndex >= 0 && relocIndex != sections.Count - 1)
			//	throw new InvalidOperationException("Reloc section must be the last section, use AddSection() to add a section");
			uint fileAlignment = 8;
			uint sectionAlignment = 8;
			CalculateRvasAndFileOffsets(chunks, 0, 0, fileAlignment, sectionAlignment);
			OnWriterEvent(ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			InitializeChunkProperties();

			OnWriterEvent(ModuleWriterEvent.BeginWriteChunks);
			var writer = new DataWriter(destStream);
			WriteChunks(writer, chunks, 0, fileAlignment);
			long imageLength = writer.Position - destStreamBaseOffset;
			OnWriterEvent(ModuleWriterEvent.EndWriteChunks);

			//OnWriterEvent(ModuleWriterEvent.BeginStrongNameSign);
			//if (Options.StrongNameKey is not null)
			//	StrongNameSign((long)strongNameSignature.FileOffset);
			//OnWriterEvent(ModuleWriterEvent.EndStrongNameSign);

			//OnWriterEvent(ModuleWriterEvent.BeginWritePEChecksum);
			//if (Options.AddCheckSum)
			//	peHeaders.WriteCheckSum(writer, imageLength);
			//OnWriterEvent(ModuleWriterEvent.EndWritePEChecksum);

			return imageLength;
		}

		void InitializeChunkProperties() {
			header.entryPointToken = GetEntryPoint();
			header.metadataRva = metadata.RVA;
			header.metadataSize = metadata.GetVirtualSize();

			//header.imageCor20Header = imageCor20Header;
			//importAddressTable.ImportDirectory = importDirectory;
			//importDirectory.ImportAddressTable = importAddressTable;
			//startupStub.ImportDirectory = importDirectory;
			//startupStub.PEHeaders = peHeaders;
			//peHeaders.StartupStub = startupStub;
			//peHeaders.ImageCor20Header = imageCor20Header;
			//peHeaders.ImportAddressTable = importAddressTable;
			//peHeaders.ImportDirectory = importDirectory;
			//peHeaders.Win32Resources = win32Resources;
			//peHeaders.RelocDirectory = relocDirectory;
			//peHeaders.DebugDirectory = debugDirectory;
			//imageCor20Header.Metadata = metadata;
			//imageCor20Header.NetResources = netResources;
			//imageCor20Header.StrongNameSignature = strongNameSignature;
			//managedExportsWriter.InitializeChunkProperties();
		}

		uint GetEntryPoint() {
			if (module.ManagedEntryPoint is MethodDef methodEntryPoint)
				return new MDToken(Table.Method, metadata.GetRid(methodEntryPoint)).Raw;
			//if (module.ManagedEntryPoint is FileDef fileEntryPoint)
			//	return new MDToken(Table.File, metadata.GetRid(fileEntryPoint)).Raw;

			//uint nativeEntryPoint = (uint)module.NativeEntryPoint;
			//if (nativeEntryPoint != 0)
			//	return nativeEntryPoint;

			return 0;
		}
	}
}
