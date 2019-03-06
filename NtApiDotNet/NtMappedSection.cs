﻿//  Copyright 2018 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;

namespace NtApiDotNet
{
    /// <summary>
    /// Class representing a mapped section
    /// </summary>
    public sealed class NtMappedSection : SafeBufferGeneric
    {
        #region Public Properties
        /// <summary>
        /// The process which the section is mapped into
        /// </summary>
        public NtProcess Process { get; private set; }

        /// <summary>
        /// The valid length of the mapped section from the current position.
        /// </summary>
        /// <remarks>This doesn't take into account the possibility of fragmented commits.</remarks>
        public long ValidLength
        {
            get
            {
                var mem_info = NtVirtualMemory.QueryMemoryInformation(NtProcess.Current.Handle, handle.ToInt64());
                if (mem_info.State == MemoryState.Commit)
                {
                    return mem_info.RegionSize;
                }
                return 0;
            }
        }

        /// <summary>
        /// Get full path for mapped section.
        /// </summary>
        public string FullPath
        {
            get
            {
                var name = NtVirtualMemory.QuerySectionName(Process.Handle, DangerousGetHandle().ToInt64(), false);
                if (name.IsSuccess)
                {
                    return name.Result;
                }
                return String.Empty;
            }
        }

        /// <summary>
        /// Query the memory protection setting for this mapping.
        /// </summary>
        public MemoryAllocationProtect Protection
        {
            get
            {
                return NtVirtualMemory.QueryMemoryInformation(Process.Handle, DangerousGetHandle().ToInt64()).Protect;
            }
        }

        /// <summary>
        /// Get image signing level.
        /// </summary>
        public SigningLevel ImageSigningLevel => NtVirtualMemory.QueryImageInformation(Process.Handle, DangerousGetHandle().ToInt64()).ImageSigningLevel;

        #endregion

        #region Constructors
        internal NtMappedSection(IntPtr pointer, long size, NtProcess process, bool writable)
            : base(pointer, size, true, writable)
        {
            if (process.Handle.IsInvalid)
            {
                // No point duplicating an invalid handle. 
                // Also covers case of pseudo current process handle.
                Process = process;
            }
            else
            {
                Process = process.Duplicate();
            }
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Release the internal handle
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            bool ret = false;
            if (!Process.Handle.IsClosed)
            {
                using (Process)
                {
                    ret = NtSystemCalls.NtUnmapViewOfSection(Process.Handle, handle).IsSuccess();
                }
            }
            handle = IntPtr.Zero;
            return ret;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if this mapped view represents the same file.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <param name="throw_on_error">True to throw on error.</param>
        /// <returns>True if the mapped view represents the same file.</returns>
        public NtResult<bool> IsSameMapping(long address, bool throw_on_error)
        {
            return NtVirtualMemory.AreMappedFilesTheSame(DangerousGetHandle().ToInt64(), address, throw_on_error);
        }

        /// <summary>
        /// Checks if this mapped view represents the same file.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <returns>True if the mapped view represents the same file.</returns>
        public bool IsSameMapping(long address)
        {
            return NtVirtualMemory.AreMappedFilesTheSame(DangerousGetHandle().ToInt64(), address);
        }

        #endregion
    }
}