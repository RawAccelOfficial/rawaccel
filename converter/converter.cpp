#include <rawaccel.hpp>

#include <array>
#include <charconv>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <optional>
#include <sstream>
#include <string>

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace Newtonsoft::Json;

namespace ra = rawaccel;
namespace fs = std::filesystem;

const wchar_t* IA_SETTINGS_NAME = L"settings.txt";
const wchar_t* IA_PROFILE_EXT = L".profile";

enum IA_MODES_ENUM { IA_QL, IA_NAT, IA_LOG };

constexpr std::array<std::string_view, 3> IA_MODES = {
    "QuakeLive", "Natural", "Logarithmic"
};

// trim from start (in place)
static inline void ltrim(std::string& s) {
    s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](unsigned char ch) {
        return !std::isspace(ch);
    }));
}

// trim from end (in place)
static inline void rtrim(std::string& s) {
    s.erase(std::find_if(s.rbegin(), s.rend(), [](unsigned char ch) {
        return !std::isspace(ch);
    }).base(), s.end());
}

// trim from both ends (in place)
static inline void trim(std::string& s) {
    ltrim(s);
    rtrim(s);
}

bool ask(std::string_view question) {
    std::cout << question << " (Y/N)" << std::endl;
    wchar_t ch;
    bool yes;
    do
    {
        ch = towupper(_getwch());
        yes = ch == 'Y';
    } while (ch != 'N' && !yes);
    return yes;
}

using ia_settings_t = std::vector<std::pair<std::string, double>>;

ia_settings_t parse_ia_settings(const fs::path fp) {
    ia_settings_t kv_pairs;

    std::ifstream ifs(fp);
    std::string line;

    while (std::getline(ifs, line)) {
        if (line.empty()) continue;

        auto delim_pos = line.find('=');
        if (delim_pos == std::string::npos) continue;

        std::string key(line.substr(0, delim_pos));
        trim(key);

        auto val_pos = line.find_first_not_of(" \t", delim_pos + 1);
        if (val_pos == std::string::npos) continue;

        double val = 0;

        auto [p, ec] = std::from_chars(&line[val_pos], &line[0] + line.size(), val);

        if (ec == std::errc()) {
            kv_pairs.emplace_back(key, val);
        }
        else if (key == "AccelMode") {
            std::string mode_val = line.substr(val_pos);
            rtrim(mode_val);
            auto it = std::find(IA_MODES.begin(), IA_MODES.end(), mode_val);
            if (it != IA_MODES.end()) {
                val = static_cast<double>(std::distance(IA_MODES.begin(), it));
                kv_pairs.emplace_back(key, val);
            }
        }
    }

    return kv_pairs;
}

auto make_extractor(const ia_settings_t& ia_settings) {
    return [&](auto... keys) -> std::optional<double> {
        auto it = std::find_if(ia_settings.begin(), ia_settings.end(), [=](auto&& p) {
            return ((p.first == keys) || ...);
        });
        if (it == ia_settings.end()) return std::nullopt;
        return it->second;
    };
}

ra::accel_args convert_natural(const ia_settings_t& ia_settings, bool legacy) {
    auto get = make_extractor(ia_settings);

    double accel = get("Acceleration").value_or(0);
    double cap = get("SensitivityCap").value_or(0);
    double sens = get("Sensitivity").value_or(1);

    ra::accel_args args;

    args.limit = 1 + std::abs(cap - sens) / sens;
    args.decay_rate = accel / sens;
    args.input_offset = get("Offset").value_or(0);
    args.mode = ra::accel_mode::natural;
    args.gain = !legacy;

    return args;
}

ra::accel_args convert_quake(const ia_settings_t& ia_settings, bool legacy) {
    auto get = make_extractor(ia_settings);

    double power = get("Power").value_or(2);
    double accel = get("Acceleration").value_or(0);
    double cap = get("SensitivityCap").value_or(0);
    double sens = get("Sensitivity").value_or(1);
    
    ra::accel_args args;

    double accel_b = std::pow(accel, power - 1) / sens;
    args.acceleration = std::pow(accel_b, 1 / (power - 1));
    args.cap.y = cap / sens;
    args.exponent_classic = power;
    args.input_offset = get("Offset").value_or(0);
    args.mode = ra::accel_mode::classic;
    args.cap_mode = ra::cap_mode::out;
    args.gain = !legacy;

    return args;
}

bool try_convert(const ia_settings_t& ia_settings) {
    auto get = make_extractor(ia_settings);

    auto& prof = *(new ra::profile());

    vec2d prescale = { get("Pre-ScaleX").value_or(1), get("Pre-ScaleY").value_or(1) };

    prof.domain_weights = prescale;
    prof.degrees_rotation = -1 * get("Angle", "AngleAdjustment").value_or(0);
    prof.sensitivity = get("Post-ScaleX").value_or(1) * prescale.x;
    prof.yx_sens_ratio = get("Post-ScaleY").value_or(1) * prescale.y / prof.sensitivity;
    prof.degrees_snap = get("AngleSnapping").value_or(0);

    double mode = get("AccelMode").value_or(IA_QL);

    switch (static_cast<IA_MODES_ENUM>(mode)) {
    case IA_QL: {
        prof.accel_x = convert_quake(ia_settings, 1);
        break;
    }
    case IA_NAT: {
        prof.accel_x = convert_natural(ia_settings, 1);
        break;
    }
    case IA_LOG: {
        std::cout << "Logarithmic accel mode is not supported.\n";
        return true;
    }
    default: return false;
    }

    auto cfg = DriverConfig::FromProfile(Marshal::PtrToStructure<Profile^>(IntPtr(&prof)));

    if (String^ errors = cfg->Errors(); errors) {
        Console::WriteLine("Bad settings: {0}", errors);
        return false;
    }

    bool nat = prof.accel_x.mode == ra::accel_mode::natural;
    bool nat_or_capped = nat || prof.accel_x.cap.y > 0;

    if (nat_or_capped) {
        Console::WriteLine("NOTE:\n"
            "    Raw Accel features a new cap style that is preferred by many users.\n"
            "    To test it out, run rawaccel.exe, check the 'Gain' option, and click 'Apply'.\n");
    }

    if (prof.accel_x.input_offset > 0) {
        Console::WriteLine("NOTE:\n"
            "    Offsets in Raw Accel work a bit differently compared to InterAccel,\n"
            "    the '{0}' parameter may need adjustment to compensate.\n",
            nat ? "decay rate" : "acceleration");
    }

    Console::Write("Sending to driver... ");
    cfg->Activate();
    Console::WriteLine("done");

    Console::Write("Generating settings.json... ");
    File::WriteAllText("settings.json", cfg->ToJSON());
    Console::WriteLine("done");

    return true;
}

int main(int argc, char** argv)
{
    auto close_prompt = [] {
        std::cout << "Press any key to close this window . . ." << std::endl;
        _getwch();
        std::exit(0);
    };

    auto convert_or_print_error = [](auto&& path) {
        try {
            if (!try_convert(parse_ia_settings(path)))
                std::cout << "Unable to convert settings.\n";
        }
        catch (Exception^ e) {
            Console::WriteLine("\nError: {0}", e);
        }
        catch (const std::exception& e) {
            std::cout << "Error: " << e.what() << '\n';
        }
    };

    try {
        VersionHelper::ValidOrThrow();
    }
    catch (InteropException^ ex) {
        Console::WriteLine(ex->Message);
        close_prompt();
    }

    if (argc == 2 && fs::exists(argv[1])) {
        convert_or_print_error(argv[1]);
    }
    else {
        std::optional<fs::path> opt_path;

        if (fs::exists(IA_SETTINGS_NAME)) {
            opt_path = IA_SETTINGS_NAME;
        }
        else {
            for (auto&& entry : fs::directory_iterator(".")) {
                if (fs::is_regular_file(entry) &&
                    entry.path().extension() == IA_PROFILE_EXT) {
                    opt_path = entry;
                    break;
                }
            }
        }

        if (opt_path) {
            std::string path = opt_path->filename().generic_string();
            std::stringstream ss;
            ss << "Found " << path <<
                "\n\nConvert and send settings generated from " << path << '?';
            if (ask(ss.str())) {
                convert_or_print_error(opt_path.value());
            }
        }
        else {
            std::cout << "Drop your InterAccel settings/profile into this directory.\n"
                "Then run this program to generate the equivalent Raw Accel settings.\n";
        }
    }

    close_prompt();
}
