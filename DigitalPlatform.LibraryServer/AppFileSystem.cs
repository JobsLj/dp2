﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DigitalPlatform.Range;
using DigitalPlatform.Text;
using DigitalPlatform.IO;
using System.Diagnostics;
using Ionic.Zip;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// 本部分是文件系统相关的代码
    /// </summary>
    public partial class LibraryApplication
    {
        // 得到 newdata 字段对应的文件名
        public static string GetNewFileName(string strFilePath)
        {
            return strFilePath + ".new~";
        }

        // 得到 range 字段对应的文件名
        public static string GetRangeFileName(string strFilePath)
        {
            return strFilePath + ".range~";
        }

        // 得到 timestamp 字段对应的文件名
        public static string GetTimestampFileName(string strFilePath)
        {
            return strFilePath + ".timestamp~";
        }

        // 观察一个 range，看是不是文件的第一个范围
        // parameters:
        //      bEndWrite   是否正好覆盖了 lTotalLength 范围
        static bool IsFirstRange(string strRange,
            long lTotalLength,
            out bool bEndWrite)
        {
            bEndWrite = false;
            RangeList range = new RangeList(strRange);
            if (range.Count == 0)
                return false;

            // 排序
            range.Sort();

            // 合并事项
            range.Merge();

            if (range.Count == 1)
            {
                RangeItem item = (RangeItem)range[0];

                if (item.lStart == 0
                    && item.lLength == lTotalLength)
                    bEndWrite = true;	// 表示完全覆盖
            }

            if (range[0].lStart == 0)
                return true;

            return false;
        }

        // 上传本地文件
        // return:
        //      -2  时间戳不匹配
        //      -1  一般性错误
        //      0   成功
        public int WriteFile(
            string strFilePath,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            string strStyle,
            byte[] baInputTimestamp,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";

            if (String.IsNullOrEmpty(strFilePath) == true)
            {
                strError = "strFilePath 参数值不能为空";
                return -1;
            }
            if (lTotalLength < 0)
            {
                strError = "lTotalLength 参数值不能为负数";
                return -1;
            }
            if (baSource == null)
            {
                strError = "baSource 参数值不能为 null";
                return -1;
            }

            if (strStyle == null)
                strStyle = "";

            bool bDelete = StringUtil.IsInList("delete", strStyle) == true;

            // 确保文件的路径所经过的所有子目录已经创建
            PathUtil.CreateDirIfNeed(Path.GetDirectoryName(strFilePath));

            string strNewFilePath = GetNewFileName(strFilePath);

            //*************************************************
            // 检查时间戳,当目标文件存在时
            if (File.Exists(strFilePath) == true
                || File.Exists(strNewFilePath) == true)
            {
                if (StringUtil.IsInList("ignorechecktimestamp", strStyle) == false)
                {
                    if (File.Exists(strNewFilePath) == true)
                        baOutputTimestamp = FileUtil.GetFileTimestamp(strNewFilePath);
                    else
                        baOutputTimestamp = FileUtil.GetFileTimestamp(strFilePath);
                    if (ByteArray.Compare(baOutputTimestamp, baInputTimestamp) != 0)
                    {
                        strError = "时间戳不匹配";
                        return -2;
                    }
                }
            }
            else
            {
                if (bDelete == true)
                {
                    string strRangeFileName = GetRangeFileName(strFilePath);

                    if (File.Exists(strRangeFileName) == true)
                        File.Delete(strRangeFileName);

                    return 0;
                }
                // 创建空文件
                using (FileStream s = File.Create(strFilePath))
                {
                }
                baOutputTimestamp = FileUtil.GetFileTimestamp(strFilePath);
            }

            // 删除文件
            if (bDelete == true)
            {
                if (File.Exists(strFilePath) == true)
                    File.Delete(strFilePath);

                if (File.Exists(strNewFilePath) == true)
                    File.Delete(strNewFilePath);

                string strRangeFileName = GetRangeFileName(strFilePath);

                if (File.Exists(strRangeFileName) == true)
                    File.Delete(strRangeFileName);

                return 0;
            }

            //**************************************************
            long lCurrentLength = 0;

            {
                if (baSource.Length == 0)
                {
                    if (strRanges != "")
                    {
                        strError = "当 baSource 参数的长度为 0 时，strRanges 的值却为 '" + strRanges + "'，不匹配，此时 strRanges 的值应为空字符串";
                        return -1;
                    }
                    // 把写到 metadata 里的尺寸设好
                    FileInfo fi = new FileInfo(strFilePath);
                    lCurrentLength = fi.Length;
                    fi = null;
                }
            }

            //******************************************
            // 写数据
            if (string.IsNullOrEmpty(strRanges) == true)
            {
                if (lTotalLength > 0)
                    strRanges = "0-" + Convert.ToString(lTotalLength - 1);
                else
                    strRanges = "";
            }
            string strRealRanges = strRanges;

            // 检查本次传来的范围是否是完整的文件。
            bool bIsComplete = false;
            if (lTotalLength == 0)
                bIsComplete = true;
            else
            {
                //		-1	出错 
                //		0	还有未覆盖的部分 
                //		1	本次已经完全覆盖
                int nState = RangeList.MergeContentRangeString(strRanges,
                    "",
                    lTotalLength,
                    out strRealRanges,
                    out strError);
                if (nState == -1)
                {
                    strError = "MergeContentRangeString() error 1 : " + strError + " (strRanges='" + strRanges + "' lTotalLength=" + lTotalLength.ToString() + ")";
                    return -1;
                }
                if (nState == 1)
                    bIsComplete = true;
            }

            if (bIsComplete == true)
            {
                if (baSource.Length != lTotalLength)
                {
                    strError = "范围 '" + strRanges + "' 与数据字节数组长度 '" + baSource.Length.ToString() + "' 不符合";
                    return -1;
                }
            }

            RangeList rangeList = new RangeList(strRealRanges);

            // 开始写数据
            Stream target = null;
            if (bIsComplete == true)
                target = File.Create(strFilePath);  //一次性发完，直接写到文件
            else
                target = File.Open(strNewFilePath, FileMode.OpenOrCreate);
            try
            {
                int nStartOfBuffer = 0;
                for (int i = 0; i < rangeList.Count; i++)
                {
                    RangeItem range = (RangeItem)rangeList[i];
                    // int nStartOfTarget = (int)range.lStart;
                    int nLength = (int)range.lLength;
                    if (nLength == 0)
                        continue;

                    Debug.Assert(range.lStart >= 0, "");

                    // 移动目标流的指针到指定位置
                    target.Seek(range.lStart,
                        SeekOrigin.Begin);

                    target.Write(baSource,
                        nStartOfBuffer,
                        nLength);

                    nStartOfBuffer += nLength;
                }
            }
            finally
            {
                target.Close();
            }

            {
                string strRangeFileName = GetRangeFileName(strFilePath);

                // 如果一次性写满的情况，需要做下列几件事情:
                // 1.时间戳以目标文件计算
                // 2.写到metadata的长度为目标文件总长度
                // 3.如果存在临时辅助文件，则删除这些文件。

                // 4. 设置目标文件的 LastWriteTime
                if (bIsComplete == true)
                {
                    // baOutputTimestamp = CreateTimestampForCfg(strFilePath);
                    lCurrentLength = lTotalLength;

                    // 删除辅助文件
                    if (File.Exists(strNewFilePath) == true)
                        File.Delete(strNewFilePath);
                    if (File.Exists(strRangeFileName) == true)
                        File.Delete(strRangeFileName);

                    goto END1;
                }


                //****************************************
                //处理辅助文件
                bool bEndWrite = false; // 是否为最后一次写入操作
                string strResultRange = "";
                if (strRanges == "" || strRanges == null)
                {
                    bEndWrite = true;
                }
                else
                {
                    string strOldRanges = "";

                    if (IsFirstRange(strRanges, lTotalLength, out bEndWrite) == false)
                    {
                        if (File.Exists(strRangeFileName) == true)
                        {
                            string strText = FileUtil.File2StringE(strRangeFileName);
                            string strOldTotalLength = "";
                            StringUtil.ParseTwoPart(strText, "|", out strOldRanges, out strOldTotalLength);
                        }
                        // return
                        //		-1	出错 
                        //		0	还有未覆盖的部分 
                        //		1	本次已经完全覆盖
                        int nState1 = RangeList.MergeContentRangeString(strRanges,
                            strOldRanges,
                            lTotalLength,
                            out strResultRange,
                            out strError);
                        if (nState1 == -1)
                        {
                            strError = "MergeContentRangeString() error 2 : " + strError + " (strRanges='" + strRanges + "' strOldRanges='" + strOldRanges + "' ) lTotalLength=" + lTotalLength.ToString() + "";
                            return -1;
                        }
                        if (nState1 == 1)
                            bEndWrite = true;
                    }
                    else
                    {
                        strResultRange = strRanges;
                    }
                }

                // 如果文件已满，需要做下列几件事情:
                // 1.按最大长度截临时文件 
                // 2.将临时文件拷到目标文件
                // 3.删除new,range辅助文件
                // 4.时间戳以目标文件计算
                // 5.metadata的长度为目标文件的总长度

                // 6. 设置目标文件的 LastWriteTime
                if (bEndWrite == true)
                {
                    using (Stream s = new FileStream(strNewFilePath,
                        FileMode.OpenOrCreate))
                    {
                        s.SetLength(lTotalLength);
                    }

                    // TODO: Move 文件较好。改名

                    File.Delete(strFilePath);
                    File.Move(strNewFilePath, strFilePath);
#if NO
                    // 用 .new 临时文件替换直接文件
                    File.Copy(strNewFilePath,
                        strFilePath,
                        true);

                    File.Delete(strNewFilePath);
#endif

                    if (File.Exists(strRangeFileName) == true)
                        File.Delete(strRangeFileName);
                    baOutputTimestamp = FileUtil.GetFileTimestamp(strFilePath);

                    lCurrentLength = lTotalLength;

                    bIsComplete = true;
                }
                else
                {

                    //如果文件未满，需要做下列几件事情：
                    // 1.把目前的range写到range辅助文件
                    // 2.时间戳以临时文件计算
                    // 3.metadata的长度为-1，即未知的情况


                    FileUtil.String2File(strResultRange + "|" + lTotalLength.ToString(),
                        strRangeFileName);

                    lCurrentLength = -1;

                    baOutputTimestamp = FileUtil.GetFileTimestamp(strNewFilePath);
                }
            }

        END1:
            if (bIsComplete == true)
            {
                // 多轮上传的内容完成后，最后需要单独设置文件最后修改时间
                string strLastWriteTime = StringUtil.GetStyleParam(strStyle, "last_write_time");
                FileUtil.SetFileLastWriteTimeByTimestamp(strFilePath, ByteArray.GetTimeStampByteArray(strLastWriteTime));
                baOutputTimestamp = FileUtil.GetFileTimestamp(strFilePath);

                // 结束时自动展开一个压缩文件
                if (StringUtil.IsInList("extractzip", strStyle) == true)
                {
                    try
                    {
                        ReadOptions option = new ReadOptions();
                        option.Encoding = Encoding.UTF8;
                        using (ZipFile zip = ZipFile.Read(strFilePath, option))
                        {
                            foreach (ZipEntry e in zip)
                            {
                                e.Extract(Path.GetDirectoryName(strFilePath), ExtractExistingFileAction.OverwriteSilently);
                            }
                        }

                        File.Delete(strFilePath);
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }
                }
            }


#if NO
            // 写metadata
            if (strMetadata != "")
            {
                string strMetadataFileName = DatabaseUtil.GetMetadataFileName(strFilePath);

                // 取出旧的数据进行合并
                string strOldMetadata = "";
                if (File.Exists(strMetadataFileName) == true)
                    strOldMetadata = FileUtil.File2StringE(strMetadataFileName);
                if (strOldMetadata == "")
                    strOldMetadata = "<file/>";

                string strResultMetadata;
                // return:
                //		-1	出错
                //		0	成功
                int nRet = DatabaseUtil.MergeMetadata(strOldMetadata,
                    strMetadata,
                    lCurrentLength,
                    out strResultMetadata,
                    out strError);
                if (nRet == -1)
                    return -1;

                // 把合并的新数据写到文件里
                FileUtil.String2File(strResultMetadata,
                    strMetadataFileName);
            }
#endif
            return 0;
        }

        // 下载本地文件
        // TODO: 限制 nMaxLength 最大值
        // return:
        //      -2      文件不存在
        //		-1      出错
        //		>= 0	成功，返回最大长度
        public static long GetFile(
            string strFilePath,
            long lStart,
            int nLength,
            int nMaxLength,
            string strStyle,
            out byte[] destBuffer,
            out byte[] outputTimestamp,
            out string strError)
        {
            destBuffer = null;
            outputTimestamp = null;
            strError = "";

            long lTotalLength = 0;
            FileInfo file = new FileInfo(strFilePath);
            if (file.Exists == false)
            {
                strError = " dp2Library 服务器不存在物理路径为 '" + strFilePath + "' 的文件";
                return -2;
            }

            // 1.取时间戳
            if (StringUtil.IsInList("timestamp", strStyle) == true)
            {
                string strNewFileName = GetNewFileName(strFilePath);
                if (File.Exists(strNewFileName) == true)
                {
                    outputTimestamp = FileUtil.GetFileTimestamp(strNewFileName);
                }
                else
                {
                    outputTimestamp = FileUtil.GetFileTimestamp(strFilePath);
                }
            }

#if NO
            // 2.取元数据
            if (StringUtil.IsInList("metadata", strStyle) == true)
            {
                string strMetadataFileName = DatabaseUtil.GetMetadataFileName(strFilePath);
                if (File.Exists(strMetadataFileName) == true)
                {
                    strMetadata = FileUtil.File2StringE(strMetadataFileName);
                }
            }
#endif

            // 3.取range
            if (StringUtil.IsInList("range", strStyle) == true)
            {
                string strRangeFileName = GetRangeFileName(strFilePath);
                if (File.Exists(strRangeFileName) == true)
                {
                    string strText = FileUtil.File2StringE(strRangeFileName);
                    string strTotalLength = "";
                    string strRange = "";
                    StringUtil.ParseTwoPart(strText, "|", out strRange, out strTotalLength);
                }
            }

            // 4.长度
            lTotalLength = file.Length;

            // 5.有data风格时,才会取数据
            if (StringUtil.IsInList("data", strStyle) == true)
            {
                if (nLength == 0)  // 取0长度
                {
                    destBuffer = new byte[0];
                    return lTotalLength;
                }
                // 检查范围是否合法
                long lOutputLength;
                // return:
                //		-1  出错
                //		0   成功
                int nRet = ConvertUtil.GetRealLength(lStart,
                    nLength,
                    lTotalLength,
                    nMaxLength,
                    out lOutputLength,
                    out strError);
                if (nRet == -1)
                    return -1;

                using(FileStream s = new FileStream(strFilePath,
                    FileMode.Open))
                {
                    destBuffer = new byte[lOutputLength];

                    Debug.Assert(lStart >= 0, "");

                    s.Seek(lStart, SeekOrigin.Begin);
                    s.Read(destBuffer,
                        0,
                        (int)lOutputLength);
                }
            }
            return lTotalLength;
        }
    }
}
