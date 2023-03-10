using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace netconfig
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public class CardInfo
        {
            public Boolean dhcp { get; set; }
            public String name { get; set; }
            public String ip { get; set; }
            public String subnet { get; set; }
            public String gateway { get; set; }
            public List<DnsInfo> dns { get; set; }
        }

        public class DnsInfo
        {
            public String ip { get; set; }
        }

        List<CardInfo> ethernetInfoList = new List<CardInfo>();
        List<CardInfo> wifiInfoList = new List<CardInfo>();

        List<DnsInfo> ethernetDnsList = new List<DnsInfo>();
        List<DnsInfo> wifiDnsList = new List<DnsInfo>();

        public MainWindow()
        {
            InitializeComponent();

            init();
        }

        private void init()
        {
            EthernetInf();
            WifiInf();
        }

        private void EthernetInf()
        {
            ethernetInfoList.Clear();
            ethernetDnsList.Clear();

            string[] NwDesc = { "TAP", "VMware", "Windows", "Virtual", "Sangfor", "Blue" };
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet && !NwDesc.Any(ni.Description.Contains))
                {
                    CardInfo ethernetInfo = new CardInfo();

                    foreach (IPAddress dnsAdress in ni.GetIPProperties().DnsAddresses)
                    {
                        if (dnsAdress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ethernetDnsList.Add(new DnsInfo { ip = dnsAdress.ToString() });
                        }
                    }
                    ethernetInfo.dns = ethernetDnsList;

                    ethernetInfo.name = ni.Description;
                    ethernetInfo.dhcp = ni.GetIPProperties().GetIPv4Properties().IsDhcpEnabled;

                    foreach (UnicastIPAddressInformation ips in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ips.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !ips.Address.ToString().StartsWith("169")) //to exclude automatic ips
                        {
                            ethernetInfo.ip = ips.Address.ToString();
                            ethernetInfo.subnet = ips.IPv4Mask.ToString();
                            break;
                        }
                    }

                    foreach (GatewayIPAddressInformation ips in ni.GetIPProperties().GatewayAddresses)
                    {
                        if (ips.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ethernetInfo.gateway = ips.Address.ToString();
                            break;
                        }
                    }

                    ethernetInfoList.Add(ethernetInfo);
                }
            }

            cb1.ItemsSource = ethernetInfoList;
            cb1.DisplayMemberPath = "name";
            cb1.SelectedValuePath = "name";
            cb1.SelectedIndex = 0;
        }

        private void WifiInf()
        {
            wifiInfoList.Clear();
            wifiDnsList.Clear();

            string[] NwDesc = { "TAP", "VMware", "Windows", "Virtual", "Sangfor", "Blue", "本地" };
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && !NwDesc.Any(ni.Description.Contains))
                {
                    CardInfo wifiInfo = new CardInfo();

                    foreach (IPAddress dnsAdress in ni.GetIPProperties().DnsAddresses)
                    {
                        if (dnsAdress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ethernetDnsList.Add(new DnsInfo { ip = dnsAdress.ToString() });
                        }
                    }
                    wifiInfo.dns = ethernetDnsList;

                    wifiInfo.dhcp = ni.GetIPProperties().GetIPv4Properties().IsDhcpEnabled;
                    wifiInfo.name = ni.Name;

                    foreach (UnicastIPAddressInformation ips in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ips.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !ips.Address.ToString().StartsWith("169")) //to exclude automatic ips
                        {
                            wifiInfo.ip = ips.Address.ToString();
                            wifiInfo.subnet = ips.IPv4Mask.ToString();
                            break;
                        }
                    }

                    foreach (GatewayIPAddressInformation ips in ni.GetIPProperties().GatewayAddresses)
                    {
                        if (ips.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            wifiInfo.gateway = ips.Address.ToString();
                            break;
                        }
                    }

                    wifiInfoList.Add(wifiInfo);
                }
            }

            cb2.ItemsSource = wifiInfoList;
            cb2.DisplayMemberPath = "name";
            cb2.SelectedValuePath = "name";
            cb2.SelectedIndex = 0;
        }

        private void OnSelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            int id = cb1.SelectedIndex;
            CardInfo cardInfo = ethernetInfoList[id];

            tb11.Text = cardInfo.ip;
            tb12.Text = cardInfo.subnet;
            tb13.Text = cardInfo.gateway;
            tb14.Clear();
            tb15.Clear();

            if (cardInfo.dns.Count() > 0) tb14.Text = cardInfo.dns[0].ip;
            if (cardInfo.dns.Count() > 1) tb15.Text = cardInfo.dns[1].ip;

            rb11.IsChecked = !cardInfo.dhcp;
            rb12.IsChecked = cardInfo.dhcp;
        }

        private void OnSelectionChanged2(object sender, SelectionChangedEventArgs e)
        {
            int id = cb2.SelectedIndex;
            CardInfo cardInfo = wifiInfoList[id];

            tb21.Text = cardInfo.ip;
            tb22.Text = cardInfo.subnet;
            tb23.Text = cardInfo.gateway;
            tb24.Clear();
            tb25.Clear();

            if (cardInfo.dns.Count() > 0) tb24.Text = cardInfo.dns[0].ip;
            if (cardInfo.dns.Count() > 1) tb25.Text = cardInfo.dns[1].ip;

            rb21.IsChecked = !cardInfo.dhcp;
            rb22.IsChecked = cardInfo.dhcp;
        }

        private void onClickYes(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("配置中，请稍候...");

            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                // 以太网
                if (mo["Description"].ToString() != "" && mo["Description"].ToString().Contains(cb1.Text))
                {
                    // 静态地址
                    if (rb11.IsChecked == true)
                    {
                        // Create an array of IP addresses and subnet masks
                        string[] ipAddresses = new string[] { tb11.Text };
                        string[] subnets = new string[] { tb12.Text };

                        // Set the IP address and subnet mask
                        mo.InvokeMethod("EnableStatic", new object[] { ipAddresses, subnets });

                        // Create an array of gateways
                        string[] gateways = new string[] { tb13.Text };

                        // Set the gateway
                        mo.InvokeMethod("SetGateways", new object[] { gateways });

                        // Create an array of dns
                        List<string> dns = new List<string>();
                        dns.Add(tb14.Text);
                        if (tb15.Text != "") dns.Add(tb15.Text);

                        // Set the dns
                        mo.InvokeMethod("SetDNSServerSearchOrder", new object[] { dns.ToArray() });
                    }
                    // DHCP
                    if (rb12.IsChecked == true)
                    {
                        mo.InvokeMethod("EnableDHCP", null);
                        mo.InvokeMethod("SetDNSServerSearchOrder", null);
                    }
                }

                // 无线网
                if (mo["Description"].ToString() != "" && mo["Description"].ToString().Contains(cb2.Text))
                {
                    // 静态地址
                    if (rb21.IsChecked == true)
                    {
                        // Create an array of IP addresses and subnet masks
                        string[] ipAddresses = new string[] { tb21.Text };
                        string[] subnets = new string[] { tb22.Text };

                        // Set the IP address and subnet mask
                        mo.InvokeMethod("EnableStatic", new object[] { ipAddresses, subnets });

                        // Create an array of gateways
                        string[] gateways = new string[] { tb23.Text };

                        // Set the gateway
                        mo.InvokeMethod("SetGateways", new object[] { gateways });

                        // Create an array of dns
                        List<string> dns = new List<string>();
                        dns.Add(tb24.Text);
                        if (tb25.Text != "") dns.Add(tb25.Text);

                        // Set the dns
                        mo.InvokeMethod("SetDNSServerSearchOrder", new object[] { dns.ToArray() });
                    }
                    // DHCP
                    if (rb12.IsChecked == true)
                    {
                        mo.InvokeMethod("EnableDHCP", null);
                        mo.InvokeMethod("SetDNSServerSearchOrder", null);
                    }
                }
            }

            MessageBox.Show("配置完成！");

            // 立马刷新
            this.onClickRefsh(null, null);
        }

        private void setIP(string arg)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
            psi.UseShellExecute = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.Verb = "runas";
            psi.Arguments = arg;
            Process process = new Process();
            process.StartInfo = psi;
            process.Start();
            process.WaitForExit(30000);
            Thread.Sleep(3000);
        }

        private void onClickSave(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "xml files (*.xml)|*.xml";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;

            saveFileDialog.ShowDialog();
            if (saveFileDialog.FileName == "")
            {
                return;
            }
            Console.WriteLine(saveFileDialog.FileName);

            XDocument document = new XDocument();
            XElement config = new XElement("config");

            XElement ethernet = new XElement("ethernet");
            ethernet.SetElementValue("cb1", cb1.Text);
            ethernet.SetElementValue("tb11", tb11.Text);
            ethernet.SetElementValue("tb12", tb12.Text);
            ethernet.SetElementValue("tb13", tb13.Text);
            ethernet.SetElementValue("tb14", tb14.Text);
            ethernet.SetElementValue("tb15", tb15.Text);
            ethernet.SetElementValue("rb11", rb11.IsChecked);
            config.Add(ethernet);

            XElement wifi = new XElement("wifi");
            wifi.SetElementValue("cb2", cb2.Text);
            wifi.SetElementValue("tb21", tb21.Text);
            wifi.SetElementValue("tb22", tb22.Text);
            wifi.SetElementValue("tb23", tb23.Text);
            wifi.SetElementValue("tb24", tb24.Text);
            wifi.SetElementValue("tb25", tb25.Text);
            wifi.SetElementValue("rb21", rb21.IsChecked);
            config.Add(wifi);

            config.Save(saveFileDialog.FileName);

            MessageBox.Show("保存方案完成！");
        }

        private void onClickLoad(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "xml files (*.xml)|*.xml";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            openFileDialog.ShowDialog();
            if (openFileDialog.FileName == "")
            {
                return;
            }
            Console.WriteLine(openFileDialog.FileName);

            XDocument document = XDocument.Load(openFileDialog.FileName);
            XElement root = document.Root;

            XElement ethernet = root.Element("ethernet");
            cb1.Text = ethernet.Element("cb1").Value;
            tb11.Text = ethernet.Element("tb11").Value;
            tb12.Text = ethernet.Element("tb12").Value;
            tb13.Text = ethernet.Element("tb13").Value;
            tb14.Text = ethernet.Element("tb14").Value;
            tb15.Text = ethernet.Element("tb15").Value;
            if (ethernet.Element("rb11").Value == "true")
            {
                rb11.IsChecked = true;
                rb12.IsChecked = false;
            }
            else
            {
                rb11.IsChecked = false;
                rb12.IsChecked = true;
            }

            XElement wifi = root.Element("wifi");
            cb2.Text = wifi.Element("cb2").Value;
            if (cb2.Text != "")
            {
                tb21.Text = wifi.Element("tb21").Value;
                tb22.Text = wifi.Element("tb22").Value;
                tb23.Text = wifi.Element("tb23").Value;
                tb24.Text = wifi.Element("tb24").Value;
                tb25.Text = wifi.Element("tb25").Value;
                if (wifi.Element("rb21").Value == "true")
                {
                    rb21.IsChecked = true;
                    rb22.IsChecked = false;
                }
                else
                {
                    rb21.IsChecked = false;
                    rb22.IsChecked = true;
                }
            }
        }

        private void onClickRefsh(object sender, RoutedEventArgs e)
        {
            init();
            if (cb1.Items.Count > 0)
            {
                OnSelectionChanged1(null, null);
            }
            if (cb2.Items.Count > 0)
            {
                OnSelectionChanged2(null, null);
            }
            MessageBox.Show("刷新完成！");
        }

        private void onClickClose(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
